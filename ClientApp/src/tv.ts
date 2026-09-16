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
}

const required = <T extends HTMLElement>(id: string): T => {
    const element = document.getElementById(id);
    if (!element) throw new Error(`Elemento requerido no encontrado: ${id}`);
    return element as T;
};

const siteName = required("site-name");
const currentDate = required("current-date");
const currentTime = required<HTMLTimeElement>("current-time");
const connectionStatus = required("connection-status");
const connectionText = required("connection-text");
const callout = required("callout");
const calledLabel = required("called-label");
const calledTurn = required("called-turn");
const calledRoom = required("called-room");
const calledDoctor = required("called-doctor");
const calledMessage = required("called-message");
const areaRoom = required("area-room");
const areaDoctor = required("area-doctor");
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
const snapshotStorageKey = "visor-turnos:v2:last-public-snapshot";
const announcementStorageKey = "visor-turnos:last-announced-version";
let currentSnapshot: TurnosSnapshot | null = null;
let hubConnected = false;
let areaSlides: AreaSlide[] = [];
let areaSlideIndex = 0;
let areaTimer: number | null = null;

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
    connectionStatus.className = `connection connection--${kind}`;
    connectionText.textContent = text;
}

function updateHealthIndicator(): void {
    if (!currentSnapshot) {
        setConnectionState(hubConnected ? "loading" : "offline", hubConnected ? "Actualizando" : "Sin conexión");
        return;
    }
    const ageSeconds = (Date.now() - Date.parse(currentSnapshot.generatedAt)) / 1000;
    if (currentSnapshot.status === "unavailable") setConnectionState("offline", "No se pudo actualizar");
    else if (currentSnapshot.status !== "live" || ageSeconds > staleAfterSeconds || !hubConnected)
        setConnectionState("stale", "Información en actualización");
    else setConnectionState("live", "Actualizado");
}

function statusPresentation(status: TurnoEstado): { label: string; css: string } {
    switch (status) {
        case "en-espera": return { label: "En espera", css: "status--waiting" };
        case "en-atencion": return { label: "Llamando", css: "status--calling" };
        case "pendiente": return { label: "Próximo", css: "status--pending" };
        default: return { label: "Por confirmar", css: "status--unknown" };
    }
}

function createPriorityTag(compact = false): HTMLElement {
    const priority = document.createElement("span");
    priority.className = compact ? "priority-tag priority-tag--compact" : "priority-tag";
    priority.textContent = compact ? "EMA" : "Prioridad 1 · EMA";
    return priority;
}

function createStatus(item: TurnoPublico): HTMLElement {
    const view = statusPresentation(item.estado);
    const status = document.createElement("span");
    status.className = `status-pill ${view.css}`;
    status.textContent = view.label;
    return status;
}

function createAreaRow(item: TurnoPublico): HTMLElement {
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
    if (item.isMedicalExam) turnCell.append(createPriorityTag());

    row.append(turnCell, createStatus(item));
    return row;
}

function createGeneralRow(item: TurnoPublico): HTMLElement {
    const row = document.createElement("div");
    row.className = "general-row";
    row.classList.toggle("board-row--priority", item.isMedicalExam);
    row.setAttribute("role", "listitem");

    const turnCell = document.createElement("span");
    turnCell.className = "turn-cell";
    const id = document.createElement("strong");
    id.className = "turn-id";
    id.textContent = item.publicId;
    turnCell.append(id);
    if (item.isMedicalExam) turnCell.append(createPriorityTag(true));

    const destination = document.createElement("span");
    destination.className = "destination-cell";
    const doctor = document.createElement("strong");
    doctor.className = "doctor-name";
    doctor.textContent = item.medico;
    const room = document.createElement("span");
    room.className = "destination-room";
    room.textContent = item.consultorio;
    destination.append(doctor, room);

    row.append(turnCell, destination, createStatus(item));
    return row;
}

function buildAreaSlides(items: TurnoPublico[]): AreaSlide[] {
    const groups = new Map<string, { consultorio: string; medico: string; items: TurnoPublico[] }>();
    for (const item of items) {
        if (item.estado === "en-atencion") continue;
        const key = `${item.consultorio}\u001f${item.medico}`;
        const group = groups.get(key) ?? { consultorio: item.consultorio, medico: item.medico, items: [] };
        group.items.push(item);
        groups.set(key, group);
    }

    const slides: AreaSlide[] = [];
    for (const group of groups.values()) {
        for (let index = 0; index < group.items.length; index += maxVisibleRows) {
            slides.push({
                consultorio: group.consultorio,
                medico: group.medico,
                items: group.items.slice(index, index + maxVisibleRows)
            });
        }
    }
    return slides;
}

function renderAreaSlide(): void {
    if (areaSlides.length === 0) {
        areaRoom.textContent = "Sin áreas pendientes";
        areaDoctor.textContent = "La pantalla se actualizará automáticamente";
        areaCounter.textContent = "—";
        areaList.replaceChildren();
        areaEmpty.hidden = false;
        return;
    }

    const slide = areaSlides[areaSlideIndex % areaSlides.length];
    if (!slide) return;
    const fragment = document.createDocumentFragment();
    slide.items.forEach(item => fragment.append(createAreaRow(item)));
    areaRoom.textContent = slide.consultorio;
    areaDoctor.textContent = slide.medico;
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
    areaSlideIndex = 0;
    renderAreaSlide();
    if (areaSlides.length > 1) {
        areaTimer = window.setInterval(() => {
            areaSlideIndex = (areaSlideIndex + 1) % areaSlides.length;
            renderAreaSlide();
        }, areaRotationSeconds * 1_000);
    }
}

function renderGeneral(items: TurnoPublico[]): void {
    const upcoming = items.filter(item => item.estado !== "en-atencion").slice(0, maxVisibleRows);
    const fragment = document.createDocumentFragment();
    upcoming.forEach(item => fragment.append(createGeneralRow(item)));
    generalList.replaceChildren(fragment);
    generalEmpty.hidden = upcoming.length > 0;
}

function renderFreshness(generatedAt: string): void {
    const generated = new Date(generatedAt);
    freshness.textContent = Number.isNaN(generated.getTime())
        ? "Última actualización: --"
        : `Última actualización: ${shortTimeFormatter.format(generated)}`;
}

function speakCallout(item: TurnoPublico): void {
    if (!("speechSynthesis" in window) || !item.publicId || !item.consultorio) {
        return;
    }

    window.speechSynthesis.cancel();
    const announcement = new SpeechSynthesisUtterance(
        `Llamando a ${item.publicId}. Diríjase al consultorio ${item.consultorio}.`,
    );
    announcement.lang = "es-PE";
    announcement.rate = 0.9;
    announcement.pitch = 1;
    announcement.volume = 1;
    window.speechSynthesis.speak(announcement);
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
            item.shouldAnnounce === candidate.shouldAnnounce;
    });
}

function renderSnapshot(snapshot: TurnosSnapshot): void {
    siteName.textContent = snapshot.siteDisplayName;
    const called = snapshot.items.find(item => item.estado === "en-atencion") ?? null;
    if (called) {
        callout.classList.remove("callout--idle");
        callout.classList.toggle("callout--priority", called.isMedicalExam);
        calledLabel.textContent = called.isMedicalExam ? "Prioridad 1 · Examen médico" : "Llamando ahora";
        calledTurn.textContent = called.publicId;
        calledRoom.textContent = called.consultorio;
        calledDoctor.textContent = called.medico;
        calledMessage.textContent = "Pase, por favor";
    } else {
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
    const announcedTurn = snapshot.items.find(item => item.shouldAnnounce && item.estado === "en-atencion");
    if (announcedTurn && announcedVersion !== snapshot.version) {
        sessionStorage.setItem(announcementStorageKey, String(snapshot.version));
        callout.classList.remove("callout--announce");
        requestAnimationFrame(() => callout.classList.add("callout--announce"));
        speakCallout(announcedTurn);
    }
}

function applySnapshot(snapshot: TurnosSnapshot, persist = true): void {
    if (currentSnapshot && snapshot.version < currentSnapshot.version) return;
    if (currentSnapshot && snapshot.version === currentSnapshot.version) {
        const contentChanged = !hasSameVisibleContent(currentSnapshot, snapshot);
        currentSnapshot = snapshot;
        if (contentChanged) {
            renderSnapshot(snapshot);
        }
        else renderFreshness(snapshot.generatedAt);

        updateHealthIndicator();
        if (persist) {
            try { localStorage.setItem(snapshotStorageKey, JSON.stringify(snapshot)); }
            catch { /* Cache opcional. */ }
        }
        return;
    }
    currentSnapshot = snapshot;
    renderSnapshot(snapshot);
    updateHealthIndicator();
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
