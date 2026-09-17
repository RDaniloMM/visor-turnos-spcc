"use strict";
const required = (id) => {
    const element = document.getElementById(id);
    if (!element)
        throw new Error(`Elemento requerido no encontrado: ${id}`);
    return element;
};
const siteName = required("site-name");
const currentDate = required("current-date");
const currentTime = required("current-time");
const connectionStatus = document.getElementById("connection-status");
const connectionText = document.getElementById("connection-text");
const callout = required("callout");
const calledLabel = required("called-label");
const calledTurn = required("called-turn");
const calledRoom = required("called-room");
const calledDoctor = required("called-doctor");
const calledMessage = required("called-message");
const areaRoom = required("area-room");
const areaDoctor = required("area-doctor");
const areaStatus = required("area-status");
const areaCounter = required("area-counter");
const areaList = required("area-list");
const areaEmpty = required("area-empty");
const generalList = required("general-list");
const generalEmpty = required("general-empty");
const freshness = required("freshness");
const readPositiveInteger = (value, fallback) => {
    const parsed = Number.parseInt(value ?? "", 10);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
};
const staleAfterSeconds = readPositiveInteger(document.body.dataset.staleAfterSeconds, 15);
const maxVisibleRows = readPositiveInteger(document.body.dataset.maxVisibleRows, 5);
const areaRotationSeconds = readPositiveInteger(document.body.dataset.areaRotationSeconds, 8);
const snapshotStorageKey = "visor-turnos:v2:last-public-snapshot";
const announcementStorageKey = "visor-turnos:v2:last-announced-version";
let currentSnapshot = null;
let hubConnected = false;
let areaSlides = [];
let areaSlideIndex = 0;
let areaTimer = null;
const dateFormatter = new Intl.DateTimeFormat("es-PE", { weekday: "long", day: "2-digit", month: "long" });
const timeFormatter = new Intl.DateTimeFormat("es-PE", { hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false });
const shortTimeFormatter = new Intl.DateTimeFormat("es-PE", { hour: "2-digit", minute: "2-digit", hour12: false });
function tickClock() {
    const now = new Date();
    currentDate.textContent = dateFormatter.format(now);
    currentTime.textContent = timeFormatter.format(now);
    currentTime.dateTime = now.toISOString();
    updateHealthIndicator();
}
function isPublicTurn(value) {
    if (typeof value !== "object" || value === null)
        return false;
    const item = value;
    return typeof item.publicId === "string" && typeof item.consultorio === "string" &&
        typeof item.medico === "string" && typeof item.estado === "string" &&
        typeof item.priorityTier === "number" && typeof item.isPreferential === "boolean" &&
        typeof item.isMedicalExam === "boolean" && typeof item.shouldAnnounce === "boolean";
}
function isSnapshot(value) {
    if (typeof value !== "object" || value === null)
        return false;
    const item = value;
    return typeof item.version === "number" && typeof item.generatedAt === "string" &&
        typeof item.siteDisplayName === "string" && Array.isArray(item.items) && item.items.every(isPublicTurn) &&
        (item.status === "live" || item.status === "stale" || item.status === "unavailable");
}
function setConnectionState(kind, text) {
    if (!connectionStatus || !connectionText)
        return;
    connectionStatus.className = `connection connection--${kind}`;
    connectionText.textContent = text;
}
function updateHealthIndicator() {
    if (!currentSnapshot) {
        setConnectionState(hubConnected ? "loading" : "offline", hubConnected ? "Actualizando" : "Sin conexión");
        return;
    }
    const ageSeconds = (Date.now() - Date.parse(currentSnapshot.generatedAt)) / 1000;
    if (currentSnapshot.status === "unavailable")
        setConnectionState("offline", "No se pudo actualizar");
    else if (currentSnapshot.status !== "live" || ageSeconds > staleAfterSeconds || !hubConnected)
        setConnectionState("stale", "Información en actualización");
    else
        setConnectionState("live", "Actualizado");
}
function statusPresentation(status) {
    switch (status) {
        case "en-espera": return { label: "En espera", css: "status--waiting" };
        case "en-atencion": return { label: "Llamando", css: "status--calling" };
        case "pendiente": return { label: "Próximo", css: "status--pending" };
        default: return { label: "Por confirmar", css: "status--unknown" };
    }
}
function createPriorityTag(compact = false) {
    const priority = document.createElement("span");
    priority.className = compact ? "priority-tag priority-tag--compact" : "priority-tag";
    priority.textContent = compact ? "EMA" : "Prioridad 1 · EMA";
    return priority;
}
function doctorDisplayName(value) {
    const doctor = value.trim();
    return doctor.toLocaleLowerCase("es-PE").startsWith("dr.") ? doctor : `Dr. ${doctor}`;
}
function createStatus(item) {
    const view = item.isActiveCall
        ? { label: "Llamando", css: "status--calling" }
        : statusPresentation(item.estado);
    const status = document.createElement("span");
    status.className = `status-pill ${view.css}`;
    status.textContent = view.label;
    return status;
}
function createAreaRow(item) {
    const row = document.createElement("div");
    row.className = "area-row";
    row.classList.toggle("board-row--priority", item.isMedicalExam);
    row.setAttribute("role", "listitem");
    const turnCell = document.createElement("span");
    turnCell.className = "turn-cell";
    const id = document.createElement("strong");
    id.className = "turn-id";
    id.textContent = item.publicId;
    turnCell.append(id);
    if (item.isMedicalExam)
        turnCell.append(createPriorityTag());
    row.append(turnCell, createStatus(item));
    return row;
}
function createGeneralRow(item) {
    const row = document.createElement("div");
    row.className = "general-row";
    row.setAttribute("role", "listitem");
    const turnCell = document.createElement("span");
    turnCell.className = "turn-cell";
    const id = document.createElement("strong");
    id.className = "turn-id";
    id.textContent = item.publicId;
    turnCell.append(id);
    const destination = document.createElement("span");
    destination.className = "destination-cell";
    const doctor = document.createElement("strong");
    doctor.className = "doctor-name";
    doctor.textContent = doctorDisplayName(item.medico);
    const room = document.createElement("span");
    room.className = "destination-room";
    room.textContent = item.consultorio;
    destination.append(doctor, room);
    row.append(turnCell, destination, createStatus(item));
    return row;
}
function buildAreaSlides(items) {
    const groups = new Map();
    for (const item of items) {
        const key = `${item.consultorio}\u001f${item.medico}`;
        const group = groups.get(key) ?? {
            consultorio: item.consultorio,
            medico: item.medico,
            items: [],
            isAttending: false,
            isCalling: false
        };
        group.isAttending ||= item.estado === "en-atencion";
        group.isCalling ||= item.isActiveCall && item.estado !== "en-atencion";
        if (item.estado !== "en-atencion")
            group.items.push(item);
        groups.set(key, group);
    }
    const slides = [];
    for (const group of groups.values()) {
        const status = group.isAttending ? "atendiendo" : group.isCalling ? "llamando" : "disponible";
        const pageCount = Math.max(1, Math.ceil(group.items.length / maxVisibleRows));
        for (let page = 0; page < pageCount; page++) {
            const index = page * maxVisibleRows;
            slides.push({
                consultorio: group.consultorio,
                medico: group.medico,
                items: group.items.slice(index, index + maxVisibleRows),
                status
            });
        }
    }
    return slides;
}
function renderAreaSlide() {
    if (areaSlides.length === 0) {
        areaRoom.textContent = "Sin áreas pendientes";
        areaDoctor.textContent = "La pantalla se actualizará automáticamente";
        areaStatus.textContent = "SIN DATOS";
        areaStatus.className = "area-status area-status--available";
        areaCounter.textContent = "—";
        areaList.replaceChildren();
        areaEmpty.hidden = false;
        return;
    }
    const slide = areaSlides[areaSlideIndex % areaSlides.length];
    if (!slide)
        return;
    const fragment = document.createDocumentFragment();
    slide.items.forEach(item => fragment.append(createAreaRow(item)));
    areaRoom.textContent = slide.consultorio;
    areaDoctor.textContent = doctorDisplayName(slide.medico);
    areaStatus.textContent = slide.status.toUpperCase();
    areaStatus.className = `area-status area-status--${slide.status}`;
    areaCounter.textContent = `${areaSlideIndex + 1} / ${areaSlides.length}`;
    areaList.replaceChildren(fragment);
    areaEmpty.hidden = true;
}
function restartAreaRotation(items) {
    if (areaTimer !== null) {
        window.clearInterval(areaTimer);
        areaTimer = null;
    }
    areaSlides = buildAreaSlides(items);
    areaSlideIndex = 0;
    renderAreaSlide();
    if (areaSlides.length > 1) {
        areaTimer = window.setInterval(() => {
            areaSlideIndex = (areaSlideIndex + 1) % areaSlides.length;
            renderAreaSlide();
        }, areaRotationSeconds * 1_000);
    }
}
function renderGeneral(items) {
    const upcoming = items.filter(item => !item.isActiveCall && item.estado !== "en-atencion").slice(0, maxVisibleRows);
    const fragment = document.createDocumentFragment();
    upcoming.forEach(item => fragment.append(createGeneralRow(item)));
    generalList.replaceChildren(fragment);
    generalEmpty.hidden = upcoming.length > 0;
}
function renderFreshness(generatedAt) {
    const generated = new Date(generatedAt);
    freshness.textContent = Number.isNaN(generated.getTime())
        ? "Última actualización: --"
        : `Última actualización: ${shortTimeFormatter.format(generated)}`;
}
function selectPeruvianSpanishVoice(voices) {
    const spanish = voices.filter(voice => voice.lang.toLocaleLowerCase().startsWith("es"));
    const exactPeruvian = spanish.find(voice => voice.lang.toLocaleLowerCase() === "es-pe");
    if (exactPeruvian)
        return exactPeruvian;
    const namedPeruvian = spanish.find(voice => /per[uú]/i.test(voice.name));
    if (namedPeruvian)
        return namedPeruvian;
    const latinAmerican = ["es-us", "es-mx", "es-co", "es-cl"];
    for (const locale of latinAmerican) {
        const voice = spanish.find(candidate => candidate.lang.toLocaleLowerCase() === locale);
        if (voice)
            return voice;
    }
    return spanish.find(voice => voice.lang.toLocaleLowerCase() !== "es-ar") ?? spanish[0];
}
function speakCallout(item) {
    if (!("speechSynthesis" in window) || !("SpeechSynthesisUtterance" in window) || !item.publicId || !item.consultorio) {
        return;
    }
    const synthesizer = window.speechSynthesis;
    let spoken = false;
    const play = () => {
        if (spoken)
            return;
        spoken = true;
        synthesizer.cancel();
        synthesizer.resume();
        const announcement = new SpeechSynthesisUtterance(`Llamando a ${item.publicId}. Diríjase al consultorio ${item.consultorio}.`);
        announcement.lang = "es-PE";
        announcement.rate = 0.9;
        announcement.pitch = 1;
        announcement.volume = 1;
        const spanishVoice = selectPeruvianSpanishVoice(synthesizer.getVoices());
        if (spanishVoice)
            announcement.voice = spanishVoice;
        synthesizer.speak(announcement);
    };
    if (synthesizer.getVoices().length === 0) {
        synthesizer.addEventListener("voiceschanged", play, { once: true });
        window.setTimeout(play, 500);
        return;
    }
    play();
}
function hasSameVisibleContent(current, next) {
    if (current.siteDisplayName !== next.siteDisplayName || current.status !== next.status || current.items.length !== next.items.length) {
        return false;
    }
    return current.items.every((item, index) => {
        const candidate = next.items[index];
        return candidate !== undefined &&
            item.publicId === candidate.publicId &&
            item.consultorio === candidate.consultorio &&
            item.medico === candidate.medico &&
            item.estado === candidate.estado &&
            item.priorityTier === candidate.priorityTier &&
            item.isPreferential === candidate.isPreferential &&
            item.isMedicalExam === candidate.isMedicalExam &&
            item.scheduledAt === candidate.scheduledAt &&
            item.arrivedAt === candidate.arrivedAt &&
            item.shouldAnnounce === candidate.shouldAnnounce &&
            item.isActiveCall === candidate.isActiveCall;
    });
}
function renderSnapshot(snapshot) {
    siteName.textContent = snapshot.siteDisplayName;
    const called = snapshot.items.find(item => item.isActiveCall) ?? null;
    if (called) {
        const isAttending = called.estado === "en-atencion";
        callout.classList.remove("callout--idle");
        callout.classList.toggle("callout--priority", called.isMedicalExam);
        calledLabel.textContent = isAttending
            ? "ATENDIENDO"
            : called.isMedicalExam ? "Prioridad 1 · Examen médico" : "LLAMANDO AHORA";
        calledTurn.textContent = called.publicId;
        calledRoom.textContent = called.consultorio;
        calledDoctor.textContent = doctorDisplayName(called.medico);
        calledMessage.textContent = isAttending ? "Atención en curso" : "Pase, por favor";
    }
    else {
        callout.classList.add("callout--idle");
        callout.classList.remove("callout--priority");
        calledLabel.textContent = "Llamando ahora";
        calledTurn.textContent = snapshot.items.length > 0 ? "Próximo llamado" : "Sin llamados pendientes";
        calledRoom.textContent = "—";
        calledDoctor.textContent = "—";
        calledMessage.textContent = snapshot.items.length > 0 ? "Permanezca atento a la pantalla" : "La lista se actualizará automáticamente";
    }
    renderGeneral(snapshot.items);
    restartAreaRotation(snapshot.items);
    renderFreshness(snapshot.generatedAt);
    const announcedVersion = Number.parseInt(sessionStorage.getItem(announcementStorageKey) ?? "-1", 10);
    const announcedTurn = snapshot.items.find(item => item.shouldAnnounce && item.isActiveCall);
    if (announcedTurn && announcedVersion !== snapshot.version) {
        sessionStorage.setItem(announcementStorageKey, String(snapshot.version));
        callout.classList.remove("callout--announce");
        requestAnimationFrame(() => callout.classList.add("callout--announce"));
        speakCallout(announcedTurn);
    }
}
function applySnapshot(snapshot, persist = true) {
    if (currentSnapshot && snapshot.version < currentSnapshot.version) {
        const incomingGeneratedAt = Date.parse(snapshot.generatedAt);
        const currentGeneratedAt = Date.parse(currentSnapshot.generatedAt);
        if (Number.isNaN(incomingGeneratedAt) ||
            (!Number.isNaN(currentGeneratedAt) && incomingGeneratedAt <= currentGeneratedAt))
            return;
    }
    if (currentSnapshot && snapshot.version === currentSnapshot.version) {
        const contentChanged = !hasSameVisibleContent(currentSnapshot, snapshot);
        currentSnapshot = snapshot;
        if (contentChanged) {
            renderSnapshot(snapshot);
        }
        else
            renderFreshness(snapshot.generatedAt);
        if (persist) {
            try {
                localStorage.setItem(snapshotStorageKey, JSON.stringify(snapshot));
            }
            catch { }
        }
        return;
    }
    currentSnapshot = snapshot;
    renderSnapshot(snapshot);
    if (persist) {
        try {
            localStorage.setItem(snapshotStorageKey, JSON.stringify(snapshot));
        }
        catch { }
    }
}
function restoreCachedSnapshot() {
    try {
        const cached = JSON.parse(localStorage.getItem(snapshotStorageKey) ?? "null");
        if (isSnapshot(cached))
            applySnapshot({ ...cached, status: "stale" }, false);
    }
    catch { }
}
async function fetchSnapshot() {
    const response = await fetch("/api/turnos/actuales", { cache: "no-store", headers: { Accept: "application/json" } });
    if (!response.ok)
        throw new Error(`Snapshot HTTP ${response.status}`);
    const snapshot = await response.json();
    if (!isSnapshot(snapshot))
        throw new Error("Contrato de snapshot inválido");
    applySnapshot(snapshot);
}
const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/turnos")
    .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 20_000]).configureLogging(signalR.LogLevel.Warning).build();
connection.on("TurnosActualizados", (payload) => { if (isSnapshot(payload))
    applySnapshot(payload); });
connection.onreconnecting(() => { hubConnected = false; updateHealthIndicator(); });
connection.onreconnected(async () => { hubConnected = true; await fetchSnapshot().catch(updateHealthIndicator); });
connection.onclose(() => { hubConnected = false; updateHealthIndicator(); });
async function connect() {
    while (!hubConnected) {
        try {
            await connection.start();
            hubConnected = true;
            await fetchSnapshot();
        }
        catch {
            hubConnected = false;
            updateHealthIndicator();
            await new Promise(resolve => window.setTimeout(resolve, 5_000));
        }
    }
}
restoreCachedSnapshot();
tickClock();
window.setInterval(tickClock, 1_000);
window.setInterval(() => { void fetchSnapshot().catch(updateHealthIndicator); }, Math.max(5_000, staleAfterSeconds * 500));
void connect();
//# sourceMappingURL=tv.js.map