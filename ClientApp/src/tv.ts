declare const signalR: typeof import("@microsoft/signalr");

type SnapshotStatus = "live" | "stale" | "unavailable";
type TurnoEstado = "pendiente" | "en-espera" | "en-atencion" | "desconocido";

interface TurnoPublico {
    publicId: string;
    consultorio: string;
    medico: string;
    estado: TurnoEstado;
    priorityTier: number;
    isPreferential: boolean;
    isMedicalExam: boolean;
    scheduledAt: string | null;
    arrivedAt: string | null;
    shouldAnnounce: boolean;
    isActiveCall: boolean;
}

interface TurnosSnapshot {
    version: number;
    generatedAt: string;
    siteDisplayName: string;
    status: SnapshotStatus;
    items: TurnoPublico[];
}

interface AreaSlide {
    consultorio: string;
    medico: string;
    items: TurnoPublico[];
    status: "llamando" | "proximo";
}

const required = <T extends HTMLElement>(id: string): T => {
    const element = document.getElementById(id);
    if (!element) throw new Error(`Elemento requerido no encontrado: ${id}`);
    return element as T;
};

const siteName = required("site-name");
const currentDate = required("current-date");
const currentTime = required<HTMLTimeElement>("current-time");
const scheduleStatus = required("schedule-status");
const connectionStatus = document.getElementById("connection-status");
const connectionText = document.getElementById("connection-text");
const turnosMain = required<HTMLElement>("turnos-main");
const callout = required("callout");
const calledLabel = required("called-label");
const calledTurn = required("called-turn");
const calledRoom = required("called-room");
const calledDoctor = required("called-doctor");
const areaPanelElement = document.querySelector<HTMLElement>(".area-panel");
if (!areaPanelElement) throw new Error("Panel de consultorio requerido no encontrado");
const areaPanel: HTMLElement = areaPanelElement;
const areaRoom = required("area-room");
const areaDoctor = required("area-doctor");
const areaStatus = required("area-status");
const areaCounter = required("area-counter");
const areaList = required("area-list");
const areaEmpty = required("area-empty");
const generalList = required("general-list");
const generalEmpty = required("general-empty");
const freshness = required("freshness");

const readPositiveInteger = (value: string | undefined, fallback: number): number => {
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
let currentSnapshot: TurnosSnapshot | null = null;
let hubConnected = false;
let areaSlides: AreaSlide[] = [];
let areaSlideIndex = 0;
let areaTimer: number | null = null;
let lastConnectionNotice = "";

const dateFormatter = new Intl.DateTimeFormat("es-PE", { weekday: "long", day: "2-digit", month: "long" });
const timeFormatter = new Intl.DateTimeFormat("es-PE", { hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false });
const shortTimeFormatter = new Intl.DateTimeFormat("es-PE", { hour: "2-digit", minute: "2-digit", hour12: false });

function tickClock(): void {
    const now = new Date();
    currentDate.textContent = dateFormatter.format(now);
    currentTime.textContent = timeFormatter.format(now);
    currentTime.dateTime = now.toISOString();
    const hour = now.getHours();
    const schedule = hour >= morningStartHour && hour < recessStartHour
        ? { label: "Turno mañana", css: "morning" }
        : hour >= recessStartHour && hour < afternoonStartHour
            ? { label: "Receso", css: "recess" }
            : hour >= afternoonStartHour && hour < dayEndHour
                ? { label: "Turno tarde", css: "afternoon" }
                : null;
    scheduleStatus.hidden = schedule === null;
    scheduleStatus.textContent = schedule?.label ?? "";
    scheduleStatus.className = schedule === null
        ? "schedule-status"
        : `schedule-status schedule-status--${schedule.css}`;
    updateHealthIndicator();
}

function isPublicTurn(value: unknown): value is TurnoPublico {
    if (typeof value !== "object" || value === null) return false;
    const item = value as Partial<TurnoPublico>;
    return typeof item.publicId === "string" && typeof item.consultorio === "string" &&
        typeof item.medico === "string" && typeof item.estado === "string" &&
        typeof item.priorityTier === "number" && typeof item.isPreferential === "boolean" &&
        typeof item.isMedicalExam === "boolean" && typeof item.shouldAnnounce === "boolean";
}

function isSnapshot(value: unknown): value is TurnosSnapshot {
    if (typeof value !== "object" || value === null) return false;
    const item = value as Partial<TurnosSnapshot>;
    return typeof item.version === "number" && typeof item.generatedAt === "string" &&
        typeof item.siteDisplayName === "string" && Array.isArray(item.items) && item.items.every(isPublicTurn) &&
        (item.status === "live" || item.status === "stale" || item.status === "unavailable");
}

function setConnectionState(kind: "live" | "loading" | "stale" | "offline", text: string): void {
    if (!connectionStatus || !connectionText) return;
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

function updateHealthIndicator(): void {
    if (!currentSnapshot) {
        setConnectionState(hubConnected ? "loading" : "offline", hubConnected ? "Actualizando" : "Sin conexión con el visor");
        return;
    }
    const ageSeconds = (Date.now() - Date.parse(currentSnapshot.generatedAt)) / 1000;
    if (currentSnapshot.status === "unavailable") setConnectionState("offline", "Error de conexión a los turnos");
    else if (currentSnapshot.status !== "live" || ageSeconds > staleAfterSeconds)
        setConnectionState("stale", "Datos desactualizados. Reintentando conexión");
    else if (!hubConnected)
        setConnectionState("stale", "Reconectando pantalla");
    else setConnectionState("live", "Actualizado");
}

function statusPresentation(_status: TurnoEstado): { label: string; css: string } {
    return { label: "Próximo", css: "status--pending" };
}

function createPriorityTag(compact = false): HTMLElement {
    const priority = document.createElement("span");
    priority.className = compact ? "priority-tag priority-tag--compact" : "priority-tag";
    priority.textContent = compact ? "EMA" : "Prioridad 1 · EMA";
    return priority;
}

function naturalName(value: string): string {
    const normalized = value.trim().toLocaleLowerCase("es-PE")
        .replace(/(^|[\s'-])(\p{L})/gu, (_match, prefix: string, letter: string) =>
            `${prefix}${letter.toLocaleUpperCase("es-PE")}`);
    return normalized.replace(/(\s)(De|Del|La|Las|Los|Y)\b/g,
        (_match, space: string, word: string) => `${space}${word.toLocaleLowerCase("es-PE")}`);
}

function doctorDisplayName(value: string): string {
    const doctor = value.trim().replace(/^dr\.\s*/i, "");
    return `Dr. ${naturalName(doctor)}`;
}

function roomDisplayName(value: string): string {
    return naturalName(value);
}

function createStatus(item: TurnoPublico): HTMLElement {
    const view = item.isActiveCall
        ? { label: "Llamando", css: "status--calling" }
        : statusPresentation(item.estado);
    const status = document.createElement("span");
    status.className = `status-pill ${view.css}`;
    status.textContent = view.label;
    return status;
}

function createAreaRow(item: TurnoPublico, isNext: boolean): HTMLElement {
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
    if (item.isMedicalExam) turnCell.append(createPriorityTag());

    row.append(turnCell, createStatus(item));
    return row;
}

function createGeneralRow(item: TurnoPublico, position: number): HTMLElement {
    const row = document.createElement("div");
    row.className = "general-row";
    row.classList.toggle("general-row--active", item.isActiveCall);
    row.setAttribute("role", "listitem");
    row.setAttribute("aria-label", `Turno ${position}: ${item.publicId}`);

    const number = document.createElement("span");
    number.className = "queue-order";
    number.textContent = String(position);
    number.setAttribute("aria-hidden", "true");

    const turnCell = document.createElement("span");
    turnCell.className = "turn-cell";
    const id = document.createElement("strong");
    id.className = "turn-id";
    id.textContent = naturalName(item.publicId);
    turnCell.append(id);

    const destination = document.createElement("span");
    destination.className = "destination-cell";
    const doctor = document.createElement("strong");
    doctor.className = "doctor-name";
    doctor.textContent = doctorDisplayName(item.medico);
    const room = document.createElement("span");
    room.className = "destination-room";
    room.textContent = roomDisplayName(item.consultorio);
    destination.append(room, doctor);

    row.append(number, turnCell, destination, createStatus(item));
    return row;
}

function buildAreaSlides(items: TurnoPublico[]): AreaSlide[] {
    const groups = new Map<string, {
        consultorio: string;
        medico: string;
        items: TurnoPublico[];
        isCalling: boolean;
    }>();
    for (const item of items) {
        const key = `${item.consultorio}\u001f${item.medico}`;
        const group = groups.get(key) ?? {
            consultorio: item.consultorio,
            medico: item.medico,
            items: [],
            isCalling: false
        };
        group.isCalling ||= item.isActiveCall;
        if (item.estado !== "en-atencion" && !item.isActiveCall) group.items.push(item);
        groups.set(key, group);
    }

    const slides: AreaSlide[] = [];
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

function renderAreaSlide(): void {
    if (areaSlides.length === 0) {
        areaRoom.textContent = "Sin áreas pendientes";
        areaDoctor.textContent = "La pantalla se actualizará automáticamente";
        areaStatus.hidden = true;
        areaPanel.className = "board-panel area-panel area-panel--empty";
        areaCounter.textContent = "—";
        areaList.replaceChildren();
        areaEmpty.hidden = false;
        return;
    }

    const slide = areaSlides[areaSlideIndex % areaSlides.length];
    if (!slide) return;
    const fragment = document.createDocumentFragment();
    let nextAssigned = false;
    slide.items.forEach(item => {
        const isNext = !item.isActiveCall && !nextAssigned;
        nextAssigned ||= isNext;
        fragment.append(createAreaRow(item, isNext));
    });
    areaRoom.textContent = roomDisplayName(slide.consultorio);
    areaDoctor.textContent = doctorDisplayName(slide.medico);
    areaStatus.textContent = slide.status.toUpperCase();
    areaStatus.className = `area-status area-status--${slide.status}`;
    areaStatus.hidden = false;
    areaPanel.className = `board-panel area-panel area-panel--${slide.status}`;
    areaCounter.textContent = `${areaSlideIndex + 1} / ${areaSlides.length}`;
    areaList.replaceChildren(fragment);
    areaEmpty.hidden = true;
}

function restartAreaRotation(items: TurnoPublico[]): void {
    if (areaTimer !== null) {
        window.clearInterval(areaTimer);
        areaTimer = null;
    }
    areaSlides = buildAreaSlides(items);
    const activeCall = items.find(item => item.isActiveCall) ?? null;
    const activeSlideIndex = activeCall
        ? areaSlides.findIndex(slide =>
            slide.items.includes(activeCall) ||
            (slide.consultorio === activeCall.consultorio && slide.medico === activeCall.medico))
        : -1;
    areaSlideIndex = activeSlideIndex >= 0 ? activeSlideIndex : 0;
    renderAreaSlide();
    // Mientras hay un llamado activo, el panel permanece en su consultorio.
    // La rotación se reanuda automáticamente al finalizar el llamado.
    if (!activeCall && areaSlides.length > 1) {
        areaTimer = window.setInterval(() => {
            areaSlideIndex = (areaSlideIndex + 1) % areaSlides.length;
            renderAreaSlide();
        }, areaRotationSeconds * 1_000);
    }
}

function renderGeneral(items: TurnoPublico[]): void {
    const upcoming = items
        .filter(item => !item.isActiveCall && item.estado !== "en-atencion")
        .slice(0, maxVisibleRows);
    const fragment = document.createDocumentFragment();
    upcoming.forEach((item, index) => fragment.append(createGeneralRow(item, index + 1)));
    generalList.replaceChildren(fragment);
    generalEmpty.hidden = upcoming.length > 0;
}

function renderFreshness(generatedAt: string): void {
    const generated = new Date(generatedAt);
    freshness.textContent = Number.isNaN(generated.getTime())
        ? "Actualizado a las --"
        : `Actualizado a las ${shortTimeFormatter.format(generated)}`;
}

function selectPeruvianSpanishVoice(voices: SpeechSynthesisVoice[]): SpeechSynthesisVoice | undefined {
    const spanish = voices.filter(voice => voice.lang.toLocaleLowerCase().startsWith("es"));
    // Las voces locales clásicas aplican reglas fonéticas españolas de forma
    // más estable a nombres propios que algunas voces multilingües/neuronales.
    const classicLocalNames = ["sabina", "helena"];
    for (const classicName of classicLocalNames) {
        const classicVoice = spanish.find(voice =>
            voice.localService && voice.name.toLocaleLowerCase().includes(classicName));
        if (classicVoice) return classicVoice;
    }

    const exactPeruvian = spanish.find(voice =>
        voice.localService && voice.lang.toLocaleLowerCase() === "es-pe") ??
        spanish.find(voice => voice.lang.toLocaleLowerCase() === "es-pe");
    if (exactPeruvian) return exactPeruvian;

    const namedPeruvian = spanish.find(voice => voice.localService && /per[uú]/i.test(voice.name)) ??
        spanish.find(voice => /per[uú]/i.test(voice.name));
    if (namedPeruvian) return namedPeruvian;

    // Preferimos acentos latinoamericanos no argentinos cuando la TV no tiene
    // instalada una voz peruana. La voz disponible sigue dependiendo de Windows.
    const latinAmerican = ["es-us", "es-mx", "es-co", "es-cl"];
    for (const locale of latinAmerican) {
        const voice = spanish.find(candidate =>
            candidate.localService && candidate.lang.toLocaleLowerCase() === locale) ??
            spanish.find(candidate => candidate.lang.toLocaleLowerCase() === locale);
        if (voice) return voice;
    }

    return spanish.find(voice => voice.localService && voice.lang.toLocaleLowerCase() !== "es-ar") ??
        spanish.find(voice => voice.lang.toLocaleLowerCase() !== "es-ar") ??
        spanish[0];
}

let preferredSpanishVoice: SpeechSynthesisVoice | undefined;
if ("speechSynthesis" in window) {
    const refreshSpanishVoice = (): void => {
        preferredSpanishVoice = selectPeruvianSpanishVoice(window.speechSynthesis.getVoices());
    };
    refreshSpanishVoice();
    window.speechSynthesis.addEventListener("voiceschanged", refreshSpanishVoice);
}

function spanishSpeechText(value: string): string {
    const replacements: ReadonlyArray<readonly [RegExp, string]> = [
        [/\b(?:jhonatan|jhonathan|jonathan|johnathan)\b/giu, "Yonatán"],
        [/\b(?:jhon|john)\b/giu, "Yon"],
        [/\brogel\b/giu, "Rojél"],
        [/\bmiraval\b/giu, "Miravál"],
    ];
    return replacements.reduce(
        (text, [pattern, pronunciation]) => text.replace(pattern, pronunciation),
        value.trim().toLocaleLowerCase("es-PE"),
    );
}

function speakCallout(item: TurnoPublico): void {
    if (!("speechSynthesis" in window) || !("SpeechSynthesisUtterance" in window) || !item.publicId || !item.consultorio) {
        return;
    }

    const synthesizer = window.speechSynthesis;
    let spoken = false;
    const play = (): boolean => {
        if (spoken) return true;
        const spanishVoice = preferredSpanishVoice ?? selectPeruvianSpanishVoice(synthesizer.getVoices());
        if (!spanishVoice) return false;
        preferredSpanishVoice = spanishVoice;
        spoken = true;

        synthesizer.cancel();
        synthesizer.resume();
        const announcement = new SpeechSynthesisUtterance(
            `Llamando a ${spanishSpeechText(item.publicId)}. ` +
            `Diríjase al consultorio ${spanishSpeechText(item.consultorio)}.`,
        );
        announcement.lang = spanishVoice.lang || "es-PE";
        announcement.rate = 0.9;
        announcement.pitch = 1;
        announcement.volume = 1;
        announcement.voice = spanishVoice;
        synthesizer.speak(announcement);
        return true;
    };

    if (play()) return;

    const onVoicesChanged = (): void => {
        if (play()) synthesizer.removeEventListener("voiceschanged", onVoicesChanged);
    };
    synthesizer.addEventListener("voiceschanged", onVoicesChanged);
    [250, 1_000, 2_500].forEach((delay, index, retries) => {
        window.setTimeout(() => {
            if (play()) {
                synthesizer.removeEventListener("voiceschanged", onVoicesChanged);
            } else if (index === retries.length - 1) {
                synthesizer.removeEventListener("voiceschanged", onVoicesChanged);
                console.warn("[Visor de turnos] No hay una voz en español instalada; se omitió el anuncio.");
            }
        }, delay);
    });
}

function hasSameVisibleContent(current: TurnosSnapshot, next: TurnosSnapshot): boolean {
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

function renderSnapshot(snapshot: TurnosSnapshot): void {
    siteName.textContent = snapshot.siteDisplayName;
    const called = snapshot.items.find(item => item.isActiveCall) ?? null;
    if (called) {
        callout.hidden = false;
        turnosMain.classList.remove("main--no-callout");
        callout.classList.remove("callout--idle");
        callout.classList.add("callout--calling");
        callout.classList.remove("callout--attending");
        callout.classList.toggle("callout--priority", called.isMedicalExam);
        calledLabel.textContent = "LLAMANDO AHORA";
        calledTurn.textContent = naturalName(called.publicId);
        calledRoom.textContent = roomDisplayName(called.consultorio);
        calledDoctor.textContent = doctorDisplayName(called.medico);
    } else {
        callout.hidden = true;
        turnosMain.classList.add("main--no-callout");
        callout.classList.add("callout--idle");
        callout.classList.remove("callout--calling", "callout--attending");
        callout.classList.remove("callout--priority");
        calledLabel.textContent = "Llamando ahora";
        calledTurn.textContent = snapshot.items.length > 0 ? "Próximo llamado" : "Sin llamados pendientes";
        calledRoom.textContent = "—";
        calledDoctor.textContent = "—";
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

function applySnapshot(snapshot: TurnosSnapshot, persist = true): void {
    if (currentSnapshot && snapshot.version < currentSnapshot.version) {
        // Tras reiniciar el proceso, la version vuelve a comenzar. Si el servidor
        // genero este snapshot despues del que guarda el navegador, prevalece la
        // hora mas reciente para que la pantalla no quede congelada.
        const incomingGeneratedAt = Date.parse(snapshot.generatedAt);
        const currentGeneratedAt = Date.parse(currentSnapshot.generatedAt);
        if (
            Number.isNaN(incomingGeneratedAt) ||
            (!Number.isNaN(currentGeneratedAt) && incomingGeneratedAt <= currentGeneratedAt)
        ) return;
    }
    if (currentSnapshot && snapshot.version === currentSnapshot.version) {
        const contentChanged = !hasSameVisibleContent(currentSnapshot, snapshot);
        currentSnapshot = snapshot;
        if (contentChanged) {
            renderSnapshot(snapshot);
        }
        else renderFreshness(snapshot.generatedAt);

        /*updateHealthIndicator();*/
        if (persist) {
            try { localStorage.setItem(snapshotStorageKey, JSON.stringify(snapshot)); }
            catch { /* Cache opcional. */ }
        }
        return;
    }
    currentSnapshot = snapshot;
    renderSnapshot(snapshot);
    /*updateHealthIndicator();*/
    if (persist) {
        try { localStorage.setItem(snapshotStorageKey, JSON.stringify(snapshot)); }
        catch { /* Cache opcional. */ }
    }
}

function restoreCachedSnapshot(): void {
    try {
        const cached = JSON.parse(localStorage.getItem(snapshotStorageKey) ?? "null") as unknown;
        if (isSnapshot(cached)) applySnapshot({ ...cached, status: "stale" }, false);
    } catch { /* Cache inválido. */ }
}

async function fetchSnapshot(): Promise<void> {
    const response = await fetch("/api/turnos/actuales", { cache: "no-store", headers: { Accept: "application/json" } });
    if (!response.ok) throw new Error(`Snapshot HTTP ${response.status}`);
    const snapshot = await response.json() as unknown;
    if (!isSnapshot(snapshot)) throw new Error("Contrato de snapshot inválido");
    applySnapshot(snapshot);
}

const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/turnos")
    .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 20_000]).configureLogging(signalR.LogLevel.Warning).build();
connection.on("TurnosActualizados", (payload: unknown) => { if (isSnapshot(payload)) applySnapshot(payload); });
connection.onreconnecting(() => { hubConnected = false; updateHealthIndicator(); });
connection.onreconnected(async () => { hubConnected = true; await fetchSnapshot().catch(updateHealthIndicator); });
connection.onclose(() => { hubConnected = false; updateHealthIndicator(); });

async function connect(): Promise<void> {
    while (!hubConnected) {
        try {
            await connection.start();
            hubConnected = true;
            await fetchSnapshot();
        } catch {
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
