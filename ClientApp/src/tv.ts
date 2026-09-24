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
    isAmanecida?: boolean;
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
    roomPosition: number;
    roomCount: number;
    pageIndex: number;
    pageCount: number;
}

interface AreaPanelState {
    panel: HTMLElement;
    room: HTMLElement;
    doctor: HTMLElement;
    status: HTMLElement;
    counter: HTMLElement;
    list: HTMLElement;
    empty: HTMLElement;
    slides: AreaSlide[];
    slideIndex: number;
    timer: number | null;
}

interface WaitingMessage {
    label: string;
    headline: string;
    context: string;
    detail: string;
    useConfiguredSiteName?: boolean;
}

const required = <T extends HTMLElement>(id: string): T => {
    const element = document.getElementById(id);
    if (!element) throw new Error(`Elemento requerido no encontrado: ${id}`);
    return element as T;
};

const siteName = required("site-name");
const currentDate = required("current-date");
const currentTime = required<HTMLTimeElement>("current-time");
const connectionStatus = document.getElementById("connection-status");
const connectionText = document.getElementById("connection-text");
const turnosMain = required<HTMLElement>("turnos-main");
const callout = required("callout");
const calledLabel = required("called-label");
const calledTurn = required("called-turn");
const calledRoom = required("called-room");
const calledDoctor = required("called-doctor");
const createAreaPanelState = (
    panelId: string,
    roomId: string,
    doctorId: string,
    statusId: string,
    counterId: string,
    listId: string,
    emptyId: string): AreaPanelState => ({
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

const primaryAreaPanel = createAreaPanelState(
    "area-panel", "area-room", "area-doctor", "area-status", "area-counter", "area-list", "area-empty");
const secondaryAreaPanel = createAreaPanelState(
    "area-panel-secondary", "area-room-secondary", "area-doctor-secondary", "area-status-secondary",
    "area-counter-secondary", "area-list-secondary", "area-empty-secondary");
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
let waitingMessageIndex = 0;
let waitingMessageTimer: number | null = null;
let lastConnectionNotice = "";

const waitingMessages: readonly WaitingMessage[] = [
    {
        label: "Bienvenidos",
        headline: "Su atención está por comenzar",
        context: "Centro médico",
        detail: "Gracias por su paciencia",
        useConfiguredSiteName: true
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

function tickClock(): void {
    const now = new Date();
    currentDate.textContent = dateFormatter.format(now);
    currentTime.textContent = timeFormatter.format(now);
    currentTime.dateTime = now.toISOString();
    updateHealthIndicator();
}

function isPublicTurn(value: unknown): value is TurnoPublico {
    if (typeof value !== "object" || value === null) return false;
    const item = value as Partial<TurnoPublico>;
    return typeof item.publicId === "string" && typeof item.consultorio === "string" &&
        typeof item.medico === "string" && typeof item.estado === "string" &&
        typeof item.priorityTier === "number" && typeof item.isPreferential === "boolean" &&
        typeof item.isMedicalExam === "boolean" &&
        (typeof item.isAmanecida === "undefined" || typeof item.isAmanecida === "boolean") &&
        typeof item.shouldAnnounce === "boolean";
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

    const roomNames = [...new Set([...groups.values()].map(group => group.consultorio))]
        .sort((left, right) => left.localeCompare(right, "es-PE"));
    const roomPositions = new Map(roomNames.map((room, index) => [room, index + 1]));
    const slides: AreaSlide[] = [];
    const orderedGroups = [...groups.values()].sort((left, right) =>
        left.consultorio.localeCompare(right.consultorio, "es-PE") ||
        left.medico.localeCompare(right.medico, "es-PE"));
    for (const group of orderedGroups) {
        const status = group.isCalling ? "llamando" : "proximo";
        const pageCount = Math.max(1, Math.ceil(group.items.length / maxVisibleRows));
        for (let page = 0; page < pageCount; page++) {
            const index = page * maxVisibleRows;
            slides.push({
                consultorio: group.consultorio,
                medico: group.medico,
                items: group.items.slice(index, index + maxVisibleRows),
                status,
                roomPosition: roomPositions.get(group.consultorio) ?? 1,
                roomCount: roomNames.length,
                pageIndex: page,
                pageCount
            });
        }
    }
    return slides;
}

function renderAreaSlide(panelState: AreaPanelState): void {
    if (panelState.slides.length === 0) {
        panelState.room.textContent = "Esperando habilitación médica";
        panelState.doctor.textContent = "Aparecerá al detectarse un nuevo número de consulta";
        panelState.status.hidden = true;
        panelState.panel.className = "board-panel area-panel area-panel--empty";
        panelState.counter.textContent = "—";
        panelState.counter.setAttribute("aria-label", "Sin consultorios activos");
        panelState.list.replaceChildren();
        panelState.empty.hidden = false;
        return;
    }

    const slide = panelState.slides[panelState.slideIndex % panelState.slides.length];
    if (!slide) return;
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
    // El indicador cuenta consultorios, no páginas de pacientes ni médicos.
    // Si un consultorio ocupa varias páginas, permanece en la misma posición.
    panelState.counter.textContent = `${slide.roomPosition} / ${slide.roomCount}`;
    panelState.counter.setAttribute("aria-label", `Consultorio ${slide.roomPosition} de ${slide.roomCount}, página ${slide.pageIndex + 1} de ${slide.pageCount}`);
    panelState.list.replaceChildren(fragment);
    panelState.empty.hidden = true;
}

function restartAreaRotation(panelState: AreaPanelState, items: TurnoPublico[]): void {
    const previousSlide = panelState.slides[panelState.slideIndex % Math.max(1, panelState.slides.length)];
    panelState.slides = buildAreaSlides(items);
    const activeCall = items.find(item => item.isActiveCall) ?? null;
    const activeSlideIndex = activeCall
        ? panelState.slides.findIndex(slide =>
            slide.items.includes(activeCall) ||
            (slide.consultorio === activeCall.consultorio && slide.medico === activeCall.medico))
        : -1;
    const preservedSlideIndex = previousSlide
        ? panelState.slides.findIndex(slide =>
            slide.consultorio === previousSlide.consultorio &&
            slide.medico === previousSlide.medico &&
            slide.pageIndex === Math.min(previousSlide.pageIndex, slide.pageCount - 1))
        : -1;
    panelState.slideIndex = activeSlideIndex >= 0
        ? activeSlideIndex
        : preservedSlideIndex >= 0
            ? preservedSlideIndex
            : 0;
    renderAreaSlide(panelState);
    // Mientras hay un llamado activo, el panel permanece en su consultorio.
    // La rotación se reanuda automáticamente al finalizar el llamado.
    if (activeCall || panelState.slides.length <= 1) {
        if (panelState.timer !== null) {
            window.clearInterval(panelState.timer);
            panelState.timer = null;
        }
        return;
    }

    // No reiniciar el contador con cada snapshot de SignalR. Así, aun cuando
    // LOLCLI actualice una fila cada tres segundos, cada consultorio conserva
    // sus ocho segundos completos en pantalla.
    if (panelState.timer === null) {
        panelState.timer = window.setInterval(() => {
            panelState.slideIndex = (panelState.slideIndex + 1) % panelState.slides.length;
            renderAreaSlide(panelState);
        }, areaRotationSeconds * 1_000);
    }
}

function renderWaitingMessage(): void {
    const message = waitingMessages[waitingMessageIndex % waitingMessages.length];
    if (!message) return;

    callout.hidden = false;
    callout.setAttribute("aria-live", "off");
    callout.className = "callout callout--waiting";
    calledLabel.textContent = message.label;
    calledTurn.textContent = message.headline;
    calledTurn.classList.remove("called-patient--long", "called-patient--very-long");
    // SiteDisplayName se origina en Site__DisplayName del .env (o de una
    // variable de entorno real) y llega dentro del snapshot del servidor.
    // Así el mismo artefacto muestra la sede correcta sin texto fijo.
    calledRoom.textContent = message.useConfiguredSiteName
        ? currentSnapshot?.siteDisplayName || message.context
        : message.context;
    calledDoctor.textContent = message.detail;
}

function startWaitingMessageRotation(): void {
    renderWaitingMessage();
    if (waitingMessageTimer !== null) return;
    waitingMessageTimer = window.setInterval(() => {
        waitingMessageIndex = (waitingMessageIndex + 1) % waitingMessages.length;
        renderWaitingMessage();
    }, 14_000);
}

function stopWaitingMessageRotation(): void {
    if (waitingMessageTimer === null) return;
    window.clearInterval(waitingMessageTimer);
    waitingMessageTimer = null;
}

function scheduledTimestamp(item: TurnoPublico): number {
    const parsed = item.scheduledAt ? Date.parse(item.scheduledAt) : Number.NaN;
    return Number.isNaN(parsed) ? Number.MAX_SAFE_INTEGER : parsed;
}

function isInCurrentSession(item: TurnoPublico, now: Date): boolean {
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

function restartAreaPanels(items: TurnoPublico[]): void {
    const now = new Date();
    // Las citas de otra jornada permanecen en la cola del servidor. Solo se
    // ocultan de ambos paneles hasta que llegue su horario, salvo un llamado
    // activo que el médico haya generado manualmente.
    const visibleItems = items.filter(item =>
        item.isActiveCall || (item.estado !== "en-atencion" && isInCurrentSession(item, now)));
    const areaKeys = [...new Set(visibleItems.map(item => item.consultorio))]
        .sort((left, right) => left.localeCompare(right, "es-PE"));
    const splitIndex = Math.ceil(areaKeys.length / 2);
    const primaryKeys = new Set(areaKeys.slice(0, splitIndex));
    const secondaryKeys = new Set(areaKeys.slice(splitIndex));

    restartAreaRotation(
        primaryAreaPanel,
        visibleItems.filter(item => primaryKeys.has(item.consultorio)));
    restartAreaRotation(
        secondaryAreaPanel,
        visibleItems.filter(item => secondaryKeys.has(item.consultorio)));
}

function renderFreshness(generatedAt: string): void {
    const generated = new Date(generatedAt);
    freshness.textContent = Number.isNaN(generated.getTime())
        ? "Actualizado a las --"
        : `Actualizado a las ${shortTimeFormatter.format(generated)}`;
}

function selectPeruvianSpanishVoice(voices: SpeechSynthesisVoice[]): SpeechSynthesisVoice | undefined {
    const spanish = voices.filter(voice => voice.lang.toLocaleLowerCase().startsWith("es"));
    // La voz pública debe conservar pronunciación latinoamericana. Una voz
    // europea no se usa como reemplazo silencioso cuando no hay voz latina.
    const exactPeruvian = spanish.find(voice =>
        voice.localService && voice.lang.toLocaleLowerCase() === "es-pe") ??
        spanish.find(voice => voice.lang.toLocaleLowerCase() === "es-pe");
    if (exactPeruvian) return exactPeruvian;

    const namedPeruvian = spanish.find(voice => voice.localService && /per[uú]/i.test(voice.name)) ??
        spanish.find(voice => /per[uú]/i.test(voice.name));
    if (namedPeruvian) return namedPeruvian;

    // Fallback estrictamente latinoamericano; no incluir es-ES (España).
    const latinAmerican = ["es-mx", "es-co", "es-cl", "es-us", "es-ec", "es-bo", "es-ar", "es-419"];
    for (const locale of latinAmerican) {
        const voice = spanish.find(candidate =>
            candidate.localService && candidate.lang.toLocaleLowerCase() === locale) ??
            spanish.find(candidate => candidate.lang.toLocaleLowerCase() === locale);
        if (voice) return voice;
    }

    // Algunas instalaciones reportan otro código regional latino; se acepta
    // siempre que sea regional y nunca la voz genérica/española de España.
    return spanish.find(voice => voice.localService &&
        voice.lang.toLocaleLowerCase() !== "es-es" && voice.lang.toLocaleLowerCase() !== "es") ??
        spanish.find(voice =>
            voice.lang.toLocaleLowerCase() !== "es-es" && voice.lang.toLocaleLowerCase() !== "es");
}

let preferredSpanishVoice: SpeechSynthesisVoice | undefined;
let pendingSpeechItem: TurnoPublico | null = null;
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
    // Algunos perfiles de Edge bloquean el primer speak() que no nació de un
    // gesto. Se conserva el aviso para reintentarlo con un clic inocuo sobre
    // el visor mientras el llamado siga activo.
    pendingSpeechItem = item;
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

window.addEventListener("pointerdown", () => {
    const pending = pendingSpeechItem;
    if (!pending || !("speechSynthesis" in window) || window.speechSynthesis.speaking) return;
    speakCallout(pending);
}, { passive: true });

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
            item.isAmanecida === candidate.isAmanecida &&
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
    } else {
        // El aviso pendiente pertenece solo al llamado vigente. Nunca se
        // reproduce después de que el banner haya terminado.
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
