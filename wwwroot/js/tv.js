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
const turnosMain = required("turnos-main");
const callout = required("callout");
const calledLabel = required("called-label");
const calledTurn = required("called-turn");
const calledRoom = required("called-room");
const calledDoctor = required("called-doctor");
const createAreaPanelState = (panelId, roomId, doctorId, statusId, counterId, listId, emptyId) => ({
    panel: required(panelId),
    room: required(roomId),
    doctor: required(doctorId),
    status: required(statusId),
    counter: required(counterId),
    list: required(listId),
    empty: required(emptyId),
    slides: [],
    slideIndex: 0,
    timer: null
});
const primaryAreaPanel = createAreaPanelState("area-panel", "area-room", "area-doctor", "area-status", "area-counter", "area-list", "area-empty");
const secondaryAreaPanel = createAreaPanelState("area-panel-secondary", "area-room-secondary", "area-doctor-secondary", "area-status-secondary", "area-counter-secondary", "area-list-secondary", "area-empty-secondary");
const freshness = required("freshness");
const readPositiveInteger = (value, fallback) => {
    const parsed = Number.parseInt(value ?? "", 10);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
};
const staleAfterSeconds = readPositiveInteger(document.body.dataset.staleAfterSeconds, 15);
const maxVisibleRows = readPositiveInteger(document.body.dataset.maxVisibleRows, 5);
const areaRotationSeconds = readPositiveInteger(document.body.dataset.areaRotationSeconds, 8);
const morningStartHour = readPositiveInteger(document.body.dataset.morningStartHour, 7);
const recessStartHour = readPositiveInteger(document.body.dataset.recessStartHour, 12);
const afternoonStartHour = readPositiveInteger(document.body.dataset.afternoonStartHour, 14);
const dayEndHour = readPositiveInteger(document.body.dataset.dayEndHour, 18);
const snapshotStorageKey = "visor-turnos:v2:last-public-snapshot";
const announcementStorageKey = "visor-turnos:v2:last-announced-version";
let currentSnapshot = null;
let hubConnected = false;
let waitingMessageIndex = 0;
let waitingMessageTimer = null;
let lastConnectionNotice = "";
const waitingMessages = [
    {
        label: "Bienvenidos",
        headline: "Su atención está por comenzar",
        context: "Hospital SPCC Cuajone",
        detail: "Gracias por su paciencia"
    },
    {
        label: "Atención ordenada",
        headline: "Acérquese al consultorio solo cuando aparezca su nombre",
        context: "Aviso visual y sonoro",
        detail: "Espere el llamado de su turno"
    },
    {
        label: "Atención con respeto",
        headline: "Respetemos el orden de atención y los casos prioritarios",
        context: "Sala de espera",
        detail: "Nuestro equipo está preparado para atenderle"
    }
];
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
        typeof item.isMedicalExam === "boolean" &&
        (typeof item.isAmanecida === "undefined" || typeof item.isAmanecida === "boolean") &&
        typeof item.shouldAnnounce === "boolean";
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
    const isError = kind === "stale" || kind === "offline";
    connectionStatus.hidden = !isError;
    const notice = isError ? `${kind}:${text}` : "";
    if (notice && notice !== lastConnectionNotice) {
        console.warn(`[Visor de turnos] ${text}`);
    }
    lastConnectionNotice = notice;
}
function updateHealthIndicator() {
    if (!currentSnapshot) {
        setConnectionState(hubConnected ? "loading" : "offline", hubConnected ? "Actualizando" : "Sin conexión con el visor");
        return;
    }
    const ageSeconds = (Date.now() - Date.parse(currentSnapshot.generatedAt)) / 1000;
    if (currentSnapshot.status === "unavailable")
        setConnectionState("offline", "Error de conexión a los turnos");
    else if (currentSnapshot.status !== "live" || ageSeconds > staleAfterSeconds)
        setConnectionState("stale", "Datos desactualizados. Reintentando conexión");
    else if (!hubConnected)
        setConnectionState("stale", "Reconectando pantalla");
    else
        setConnectionState("live", "Actualizado");
}
function statusPresentation(_status) {
    return { label: "Próximo", css: "status--pending" };
}
function createPriorityTag(compact = false) {
    const priority = document.createElement("span");
    priority.className = compact ? "priority-tag priority-tag--compact" : "priority-tag";
    priority.textContent = compact ? "EMA" : "Prioridad 1 · EMA";
    return priority;
}
function naturalName(value) {
    const normalized = value.trim().toLocaleLowerCase("es-PE")
        .replace(/(^|[\s'-])(\p{L})/gu, (_match, prefix, letter) => `${prefix}${letter.toLocaleUpperCase("es-PE")}`);
    return normalized.replace(/(\s)(De|Del|La|Las|Los|Y)\b/g, (_match, space, word) => `${space}${word.toLocaleLowerCase("es-PE")}`);
}
function doctorDisplayName(value) {
    const doctor = value.trim().replace(/^dr\.\s*/i, "");
    return `Dr. ${naturalName(doctor)}`;
}
function roomDisplayName(value) {
    return naturalName(value);
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
function createAreaRow(item, isNext) {
    const row = document.createElement("div");
    row.className = "area-row";
    row.classList.toggle("area-row--active", item.isActiveCall);
    row.classList.toggle("area-row--next", isNext);
    row.classList.toggle("board-row--priority", item.isMedicalExam);
    row.setAttribute("role", "listitem");
    const turnCell = document.createElement("span");
    turnCell.className = "turn-cell";
    const id = document.createElement("strong");
    id.className = "turn-id";
    id.textContent = naturalName(item.publicId);
    turnCell.append(id);
    if (item.isMedicalExam)
        turnCell.append(createPriorityTag());
    row.append(turnCell, createStatus(item));
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
            isCalling: false
        };
        group.isCalling ||= item.isActiveCall;
        if (item.estado !== "en-atencion" && !item.isActiveCall)
            group.items.push(item);
        groups.set(key, group);
    }
    const slides = [];
    for (const group of groups.values()) {
        const status = group.isCalling ? "llamando" : "proximo";
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
function renderAreaSlide(panelState) {
    if (panelState.slides.length === 0) {
        panelState.room.textContent = "Esperando habilitación médica";
        panelState.doctor.textContent = "Aparecerá al detectarse un nuevo número de consulta";
        panelState.status.hidden = true;
        panelState.panel.className = "board-panel area-panel area-panel--empty";
        panelState.counter.textContent = "—";
        panelState.list.replaceChildren();
        panelState.empty.hidden = false;
        return;
    }
    const slide = panelState.slides[panelState.slideIndex % panelState.slides.length];
    if (!slide)
        return;
    const fragment = document.createDocumentFragment();
    let nextAssigned = false;
    slide.items.forEach(item => {
        const isNext = !item.isActiveCall && !nextAssigned;
        nextAssigned ||= isNext;
        fragment.append(createAreaRow(item, isNext));
    });
    panelState.room.textContent = roomDisplayName(slide.consultorio);
    panelState.doctor.textContent = doctorDisplayName(slide.medico);
    panelState.status.textContent = slide.status.toUpperCase();
    panelState.status.className = `area-status area-status--${slide.status}`;
    panelState.status.hidden = false;
    panelState.panel.className = `board-panel area-panel area-panel--${slide.status}`;
    panelState.counter.textContent = `${panelState.slideIndex + 1} / ${panelState.slides.length}`;
    panelState.list.replaceChildren(fragment);
    panelState.empty.hidden = true;
}
function restartAreaRotation(panelState, items) {
    const previousSlide = panelState.slides[panelState.slideIndex % Math.max(1, panelState.slides.length)];
    panelState.slides = buildAreaSlides(items);
    const activeCall = items.find(item => item.isActiveCall) ?? null;
    const activeSlideIndex = activeCall
        ? panelState.slides.findIndex(slide => slide.items.includes(activeCall) ||
            (slide.consultorio === activeCall.consultorio && slide.medico === activeCall.medico))
        : -1;
    const preservedSlideIndex = previousSlide
        ? panelState.slides.findIndex(slide => slide.consultorio === previousSlide.consultorio && slide.medico === previousSlide.medico)
        : -1;
    panelState.slideIndex = activeSlideIndex >= 0
        ? activeSlideIndex
        : preservedSlideIndex >= 0
            ? preservedSlideIndex
            : 0;
    renderAreaSlide(panelState);
    if (activeCall || panelState.slides.length <= 1) {
        if (panelState.timer !== null) {
            window.clearInterval(panelState.timer);
            panelState.timer = null;
        }
        return;
    }
    if (panelState.timer === null) {
        panelState.timer = window.setInterval(() => {
            panelState.slideIndex = (panelState.slideIndex + 1) % panelState.slides.length;
            renderAreaSlide(panelState);
        }, areaRotationSeconds * 1_000);
    }
}
function renderWaitingMessage() {
    const message = waitingMessages[waitingMessageIndex % waitingMessages.length];
    if (!message)
        return;
    callout.hidden = false;
    callout.setAttribute("aria-live", "off");
    callout.className = "callout callout--waiting";
    calledLabel.textContent = message.label;
    calledTurn.textContent = message.headline;
    calledTurn.classList.remove("called-patient--long", "called-patient--very-long");
    calledRoom.textContent = message.context;
    calledDoctor.textContent = message.detail;
}
function startWaitingMessageRotation() {
    renderWaitingMessage();
    if (waitingMessageTimer !== null)
        return;
    waitingMessageTimer = window.setInterval(() => {
        waitingMessageIndex = (waitingMessageIndex + 1) % waitingMessages.length;
        renderWaitingMessage();
    }, 14_000);
}
function stopWaitingMessageRotation() {
    if (waitingMessageTimer === null)
        return;
    window.clearInterval(waitingMessageTimer);
    waitingMessageTimer = null;
}
function scheduledTimestamp(item) {
    const parsed = item.scheduledAt ? Date.parse(item.scheduledAt) : Number.NaN;
    return Number.isNaN(parsed) ? Number.MAX_SAFE_INTEGER : parsed;
}
function isInCurrentSession(item, now) {
    const scheduled = new Date(scheduledTimestamp(item));
    if (Number.isNaN(scheduled.getTime()) ||
        scheduled.getFullYear() !== now.getFullYear() ||
        scheduled.getMonth() !== now.getMonth() ||
        scheduled.getDate() !== now.getDate()) {
        return false;
    }
    const currentHour = now.getHours();
    const sessionStartHour = currentHour >= morningStartHour && currentHour < recessStartHour
        ? morningStartHour
        : currentHour >= afternoonStartHour && currentHour < dayEndHour
            ? afternoonStartHour
            : null;
    const sessionEndHour = sessionStartHour === morningStartHour
        ? recessStartHour
        : sessionStartHour === afternoonStartHour
            ? dayEndHour
            : null;
    return sessionStartHour !== null && sessionEndHour !== null &&
        scheduled.getHours() >= sessionStartHour && scheduled.getHours() < sessionEndHour;
}
function restartAreaPanels(items) {
    const now = new Date();
    const visibleItems = items.filter(item => item.isActiveCall || (item.estado !== "en-atencion" && isInCurrentSession(item, now)));
    const areaKeys = [...new Set(visibleItems.map(item => `${item.consultorio}\u001f${item.medico}`))]
        .sort((left, right) => left.localeCompare(right, "es-PE"));
    const splitIndex = Math.ceil(areaKeys.length / 2);
    const primaryKeys = new Set(areaKeys.slice(0, splitIndex));
    const secondaryKeys = new Set(areaKeys.slice(splitIndex));
    restartAreaRotation(primaryAreaPanel, visibleItems.filter(item => primaryKeys.has(`${item.consultorio}\u001f${item.medico}`)));
    restartAreaRotation(secondaryAreaPanel, visibleItems.filter(item => secondaryKeys.has(`${item.consultorio}\u001f${item.medico}`)));
}
function renderFreshness(generatedAt) {
    const generated = new Date(generatedAt);
    freshness.textContent = Number.isNaN(generated.getTime())
        ? "Actualizado a las --"
        : `Actualizado a las ${shortTimeFormatter.format(generated)}`;
}
function selectPeruvianSpanishVoice(voices) {
    const spanish = voices.filter(voice => voice.lang.toLocaleLowerCase().startsWith("es"));
    const exactPeruvian = spanish.find(voice => voice.localService && voice.lang.toLocaleLowerCase() === "es-pe") ??
        spanish.find(voice => voice.lang.toLocaleLowerCase() === "es-pe");
    if (exactPeruvian)
        return exactPeruvian;
    const namedPeruvian = spanish.find(voice => voice.localService && /per[uú]/i.test(voice.name)) ??
        spanish.find(voice => /per[uú]/i.test(voice.name));
    if (namedPeruvian)
        return namedPeruvian;
    const latinAmerican = ["es-mx", "es-co", "es-cl", "es-us", "es-ec", "es-bo", "es-ar", "es-419"];
    for (const locale of latinAmerican) {
        const voice = spanish.find(candidate => candidate.localService && candidate.lang.toLocaleLowerCase() === locale) ??
            spanish.find(candidate => candidate.lang.toLocaleLowerCase() === locale);
        if (voice)
            return voice;
    }
    return spanish.find(voice => voice.localService &&
        voice.lang.toLocaleLowerCase() !== "es-es" && voice.lang.toLocaleLowerCase() !== "es") ??
        spanish.find(voice => voice.lang.toLocaleLowerCase() !== "es-es" && voice.lang.toLocaleLowerCase() !== "es");
}
let preferredSpanishVoice;
let pendingSpeechItem = null;
if ("speechSynthesis" in window) {
    const refreshSpanishVoice = () => {
        preferredSpanishVoice = selectPeruvianSpanishVoice(window.speechSynthesis.getVoices());
    };
    refreshSpanishVoice();
    window.speechSynthesis.addEventListener("voiceschanged", refreshSpanishVoice);
}
function spanishSpeechText(value) {
    const replacements = [
        [/\b(?:jhonatan|jhonathan|jonathan|johnathan)\b/giu, "Yonatán"],
        [/\b(?:jhon|john)\b/giu, "Yon"],
        [/\brogel\b/giu, "Rojél"],
        [/\bmiraval\b/giu, "Miravál"],
    ];
    return replacements.reduce((text, [pattern, pronunciation]) => text.replace(pattern, pronunciation), value.trim().toLocaleLowerCase("es-PE"));
}
function speakCallout(item) {
    if (!("speechSynthesis" in window) || !("SpeechSynthesisUtterance" in window) || !item.publicId || !item.consultorio) {
        return;
    }
    const synthesizer = window.speechSynthesis;
    pendingSpeechItem = item;
    let spoken = false;
    const play = () => {
        if (spoken)
            return true;
        const spanishVoice = preferredSpanishVoice ?? selectPeruvianSpanishVoice(synthesizer.getVoices());
        if (!spanishVoice)
            return false;
        preferredSpanishVoice = spanishVoice;
        spoken = true;
        synthesizer.cancel();
        synthesizer.resume();
        const announcement = new SpeechSynthesisUtterance(`Llamando a ${spanishSpeechText(item.publicId)}. ` +
            `Diríjase al consultorio ${spanishSpeechText(item.consultorio)}.`);
        announcement.lang = spanishVoice.lang || "es-PE";
        announcement.rate = 0.9;
        announcement.pitch = 1;
        announcement.volume = 1;
        announcement.voice = spanishVoice;
        announcement.onstart = () => { pendingSpeechItem = null; };
        announcement.onerror = () => { pendingSpeechItem = item; };
        synthesizer.speak(announcement);
        window.setTimeout(() => {
            if (!synthesizer.speaking && pendingSpeechItem?.publicId === item.publicId) {
                pendingSpeechItem = item;
            }
        }, 1_000);
        return true;
    };
    if (play())
        return;
    const onVoicesChanged = () => {
        if (play())
            synthesizer.removeEventListener("voiceschanged", onVoicesChanged);
    };
    synthesizer.addEventListener("voiceschanged", onVoicesChanged);
    [250, 1_000, 2_500].forEach((delay, index, retries) => {
        window.setTimeout(() => {
            if (play()) {
                synthesizer.removeEventListener("voiceschanged", onVoicesChanged);
            }
            else if (index === retries.length - 1) {
                synthesizer.removeEventListener("voiceschanged", onVoicesChanged);
                console.warn("[Visor de turnos] No hay una voz en español instalada; se omitió el anuncio.");
            }
        }, delay);
    });
}
window.addEventListener("pointerdown", () => {
    const pending = pendingSpeechItem;
    if (!pending || !("speechSynthesis" in window) || window.speechSynthesis.speaking)
        return;
    speakCallout(pending);
}, { passive: true });
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
            item.isAmanecida === candidate.isAmanecida &&
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
        stopWaitingMessageRotation();
        if (pendingSpeechItem && pendingSpeechItem.publicId !== called.publicId) {
            pendingSpeechItem = null;
        }
        callout.hidden = false;
        callout.setAttribute("aria-live", "assertive");
        turnosMain.classList.remove("main--no-callout");
        callout.classList.remove("callout--idle");
        callout.classList.remove("callout--waiting");
        callout.classList.add("callout--calling");
        callout.classList.remove("callout--attending");
        callout.classList.toggle("callout--priority", called.isMedicalExam);
        calledLabel.textContent = "LLAMANDO AHORA";
        const calledName = naturalName(called.publicId);
        calledTurn.textContent = calledName;
        calledTurn.classList.toggle("called-patient--long", calledName.length > 25);
        calledTurn.classList.toggle("called-patient--very-long", calledName.length > 38);
        calledRoom.textContent = roomDisplayName(called.consultorio);
        calledDoctor.textContent = doctorDisplayName(called.medico);
    }
    else {
        pendingSpeechItem = null;
        turnosMain.classList.remove("main--no-callout");
        startWaitingMessageRotation();
    }
    restartAreaPanels(snapshot.items);
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