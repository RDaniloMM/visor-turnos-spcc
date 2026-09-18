"use strict";

const tableBody = document.getElementById("turns");
const orderedTableBody = document.getElementById("ordered-turns");
const internalQueueTableBody = document.getElementById("internal-queue");
const queueFreshness = document.getElementById("queue-freshness");
const refreshButton = document.getElementById("refresh");
const feedback = document.getElementById("feedback");
const roomFilter = document.getElementById("room-filter");
const roomControls = document.getElementById("room-controls");
const detailTab = document.getElementById("detail-tab");
const orderedTab = document.getElementById("ordered-tab");
const detailView = document.getElementById("detail-view");
const orderedView = document.getElementById("ordered-view");
const createAppointmentForm = document.getElementById("create-appointment-form");
const demoMedicalCode = document.getElementById("demo-medical-code");
const demoScheduledAt = document.getElementById("demo-scheduled-at");
const demoPriority = document.getElementById("demo-priority");
const createAppointmentButton = document.getElementById("create-appointment");
const dateTimeFormatter = new Intl.DateTimeFormat("es-PE", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false
});
let loadedTurns = [];
let activeView = "detail";
let isLoading = false;

function textCell(value) {
    const cell = document.createElement("td");
    cell.textContent = value;
    return cell;
}

function actionButton(label, action, disabled) {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = label;
    button.disabled = disabled;
    button.dataset.action = action;
    return button;
}

function formatDateTime(value) {
    if (!value) return "NULL";
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? "NULL" : dateTimeFormatter.format(date);
}

function formatValue(value) {
    return value === null || value === undefined || value === "" ? "NULL" : String(value);
}

function toLocalInputValue(date) {
    const twoDigits = value => String(value).padStart(2, "0");
    return `${date.getFullYear()}-${twoDigits(date.getMonth() + 1)}-${twoDigits(date.getDate())}T${twoDigits(date.getHours())}:${twoDigits(date.getMinutes())}`;
}

function setDefaultDemoAppointmentTime() {
    if (demoScheduledAt.value) return;
    const nextSlot = new Date();
    nextSlot.setSeconds(0, 0);
    nextSlot.setMinutes(nextSlot.getMinutes() + 5);
    nextSlot.setMinutes(Math.ceil(nextSlot.getMinutes() / 5) * 5);

    const morningStartHour = Number(createAppointmentForm.dataset.morningStartHour);
    const recessStartHour = Number(createAppointmentForm.dataset.recessStartHour);
    const afternoonStartHour = Number(createAppointmentForm.dataset.afternoonStartHour);
    const dayEndHour = Number(createAppointmentForm.dataset.dayEndHour);
    const decimalHour = nextSlot.getHours() + nextSlot.getMinutes() / 60;
    if (decimalHour < morningStartHour) {
        nextSlot.setHours(morningStartHour, 0, 0, 0);
    } else if (decimalHour >= recessStartHour && decimalHour < afternoonStartHour) {
        nextSlot.setHours(afternoonStartHour, 0, 0, 0);
    } else if (decimalHour >= dayEndHour) {
        nextSlot.setHours(dayEndHour - 1, 55, 0, 0);
    }

    demoScheduledAt.value = toLocalInputValue(nextSlot);
}

function renderDemoMedicalOptions(turns) {
    const professionals = [...new Map(
        turns
            .filter(turn => turn.medicalCode)
            .map(turn => [turn.medicalCode, turn])
    ).values()]
        .sort((left, right) => `${left.consultorio} ${left.medico}`.localeCompare(`${right.consultorio} ${right.medico}`, "es-PE"));
    const previousMedicalCode = demoMedicalCode.value;
    demoMedicalCode.replaceChildren();
    for (const professional of professionals) {
        const option = document.createElement("option");
        option.value = professional.medicalCode;
        option.textContent = `${professional.consultorio} · Dr. ${professional.medico.replace(/^dr\.\s*/i, "")}`;
        demoMedicalCode.append(option);
    }
    demoMedicalCode.value = professionals.some(turn => turn.medicalCode === previousMedicalCode)
        ? previousMedicalCode
        : (professionals[0]?.medicalCode ?? "");
    createAppointmentButton.disabled = professionals.length === 0;
}

function queueFollowUp(entry) {
    if (entry.hasReachedMaxAttempts) return "Ausente · máximo alcanzado";
    if (entry.isRequeueExpired) return "Ausente · vencido";
    if (entry.isAwaitingClose) return `Esperando P/S · llamado ${entry.callAttempts}`;
    if (entry.queueState === "esperando-reactivacion") return `Espera nuevo T · ${entry.callAttempts} intento(s)`;
    if (entry.callAttempts > 0) return `Llamado ${entry.callAttempts}`;
    return "En cola";
}

function orderedQueueState(entry) {
    if (entry.queueState === "llamando") return "Llamando";
    if (entry.hasReachedMaxAttempts) return "Ausente · máximo";
    if (entry.isRequeueExpired) return "Ausente · vencido";
    if (entry.isAwaitingClose) return "Esperando cierre P/S";
    if (entry.queueState === "esperando-reactivacion") return "Esperando nuevo numcon=T";
    return entry.isEligibleForCall ? "Habilitado para llamar" : "Próximo";
}

function appendSimulationActions(row, appointmentId, hasOpenAct, hasPrefactura) {
    const actions = document.createElement("td");
    actions.className = "actions";
    actions.append(
        actionButton("Llamar", "llamar", hasOpenAct),
        actionButton("Guardar", "guardar-consulta", !hasOpenAct),
        actionButton("Restablecer", "restablecer", !hasPrefactura));
    for (const button of actions.querySelectorAll("button")) {
        button.addEventListener("click", () => void runAction(appointmentId, button.dataset.action ?? "", button));
    }
    row.append(actions);
}

function renderOrderedQueue(entries, turns) {
    const turnsByAppointment = new Map(turns.map(turn => [turn.invnum, turn]));
    const fragment = document.createDocumentFragment();
    for (const entry of entries) {
        const technicalTurn = turnsByAppointment.get(entry.appointmentId);
        const row = document.createElement("tr");
        row.append(textCell(String(entry.positionInArea)));
        row.append(textCell(`Dr. ${entry.medico.replace(/^dr\.\s*/i, "")}`));
        row.append(textCell(entry.consultorio));
        row.append(textCell(entry.patientName));
        row.append(textCell(formatDateTime(entry.scheduledAt)));
        row.append(textCell(formatValue(technicalTurn?.appointmentObservation)));
        row.append(textCell(orderedQueueState(entry)));
        row.append(textCell(formatValue(entry.prefacturaNumber)));
        row.append(textCell(entry.consultationId
            ? `${entry.consultationId} / ${technicalTurn?.consultationStatus ?? "NULL"}`
            : "NULL"));
        appendSimulationActions(
            row,
            entry.appointmentId,
            technicalTurn?.consultationStatus === "T",
            Boolean(entry.prefacturaNumber && entry.prefacturaNumber !== 0));
        fragment.append(row);
    }
    orderedTableBody.replaceChildren(fragment);
}

function renderInternalQueue(entries) {
    const fragment = document.createDocumentFragment();
    for (const entry of entries) {
        const row = document.createElement("tr");
        row.append(textCell(String(entry.positionInArea)));
        row.append(textCell(entry.patientName));
        row.append(textCell(`${entry.consultorio} · Dr. ${entry.medico.replace(/^dr\.\s*/i, "")}`));

        const state = document.createElement("td");
        const statePill = document.createElement("span");
        statePill.className = `queue-state queue-state--${entry.queueState}`;
        statePill.textContent = orderedQueueState(entry);
        state.append(statePill);
        row.append(state);

        row.append(textCell(entry.isEligibleForCall ? "Sí" : "No"));
        row.append(textCell(queueFollowUp(entry)));
        fragment.append(row);
    }
    internalQueueTableBody.replaceChildren(fragment);
    queueFreshness.textContent = `Actualizado ${new Intl.DateTimeFormat("es-PE", { hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false }).format(new Date())}`;
}

function setActiveView(view) {
    activeView = view;
    const showingDetail = view === "detail";
    detailTab.setAttribute("aria-selected", String(showingDetail));
    orderedTab.setAttribute("aria-selected", String(!showingDetail));
    detailTab.classList.toggle("view-tab--active", showingDetail);
    orderedTab.classList.toggle("view-tab--active", !showingDetail);
    detailView.hidden = !showingDetail;
    orderedView.hidden = showingDetail;
    roomControls.hidden = !showingDetail;
}

function render(turns, focusAppointmentId = null) {
    const rooms = [...new Set(turns.map(turn => turn.consultorio))].sort((left, right) => left.localeCompare(right, "es-PE"));
    const priorRoom = roomFilter.value;
    const focusedTurn = focusAppointmentId === null
        ? null
        : turns.find(turn => turn.invnum === focusAppointmentId);
    roomFilter.replaceChildren();
    const allRoomsOption = document.createElement("option");
    allRoomsOption.value = "";
    allRoomsOption.textContent = "Todos los consultorios";
    roomFilter.append(allRoomsOption);
    for (const room of rooms) {
        const option = document.createElement("option");
        option.value = room;
        option.textContent = room;
        roomFilter.append(option);
    }
    roomFilter.value = focusedTurn?.consultorio ?? (rooms.includes(priorRoom) ? priorRoom : "");
    const selectedTurns = roomFilter.value
        ? turns.filter(turn => turn.consultorio === roomFilter.value)
        : turns;
    const fragment = document.createDocumentFragment();
    for (const turn of selectedTurns) {
        const row = document.createElement("tr");
        row.append(textCell(`Dr. ${turn.medico.replace(/^dr\.\s*/i, "")}`));
        row.append(textCell(turn.consultorio));
        row.append(textCell(turn.patientName));
        row.append(textCell(formatDateTime(turn.scheduledAt)));
        row.append(textCell(formatValue(turn.appointmentStatus)));
        row.append(textCell(formatValue(turn.appointmentObservation)));
        row.append(textCell(formatValue(turn.prefacturaNumber)));
        row.append(textCell(formatValue(turn.invnum)));
        row.append(textCell(formatValue(turn.consultationId)));
        row.append(textCell(formatValue(turn.consultationStatus)));
        row.append(textCell(formatDateTime(turn.consultationConnectedAt)));
        row.append(textCell(formatDateTime(turn.consultationCreatedAt)));
        row.append(textCell(formatDateTime(turn.consultationLastModifiedAt)));

        const hasPrefactura = Boolean(turn.prefacturaNumber && turn.prefacturaNumber !== 0);
        appendSimulationActions(row, turn.invnum, turn.consultationStatus === "T", hasPrefactura);
        fragment.append(row);
    }
    tableBody.replaceChildren(fragment);
    renderDemoMedicalOptions(turns);
    setDefaultDemoAppointmentTime();
}

function showFeedback(message, isError = false) {
    feedback.textContent = message;
    feedback.className = isError ? "feedback feedback--error" : "feedback feedback--success";
}

async function loadTurns(showSuccess = true, focusAppointmentId = null) {
    if (isLoading) return;
    isLoading = true;
    refreshButton.disabled = true;
    try {
        const [turnsResponse, queueResponse] = await Promise.all([
            fetch("/api/dev/simulador/turnos", { cache: "no-store" }),
            fetch("/api/dev/simulador/cola-interna", { cache: "no-store" })
        ]);
        if (!turnsResponse.ok || !queueResponse.ok) throw new Error("No se pudo cargar la copia local.");
        const [turns, queueEntries] = await Promise.all([turnsResponse.json(), queueResponse.json()]);
        loadedTurns = turns;
        render(turns, focusAppointmentId);
        renderOrderedQueue(queueEntries, turns);
        renderInternalQueue(queueEntries);
        if (showSuccess) showFeedback(`${turns.length} citas cargadas desde la copia local.`);
    } catch (error) {
        showFeedback(error instanceof Error ? error.message : "No se pudo cargar la copia local.", true);
    } finally {
        refreshButton.disabled = false;
        isLoading = false;
    }
}

async function runAction(invnum, action, button) {
    button.disabled = true;
    try {
        const response = await fetch(`/api/dev/simulador/${invnum}/${action}`, { method: "POST" });
        const result = await response.json();
        if (!response.ok || !result.succeeded) throw new Error(result.message ?? "No se pudo ejecutar la simulación.");
        showFeedback(result.message);
        await loadTurns(false);
    } catch (error) {
        showFeedback(error instanceof Error ? error.message : "No se pudo ejecutar la simulación.", true);
    } finally {
        button.disabled = false;
    }
}

async function createFictitiousAppointment(event) {
    event.preventDefault();
    if (!demoMedicalCode.value || !demoScheduledAt.value) {
        showFeedback("Selecciona profesional y hora para la cita ficticia.", true);
        return;
    }

    createAppointmentButton.disabled = true;
    try {
        const response = await fetch("/api/dev/simulador/citas-ficticias", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                medicalCode: demoMedicalCode.value,
                scheduledAt: demoScheduledAt.value,
                priority: demoPriority.value
            })
        });
        const result = await response.json();
        if (!response.ok || !result.succeeded) throw new Error(result.message ?? "No se pudo agregar la cita ficticia.");
        showFeedback(result.message);
        setActiveView("detail");
        await loadTurns(false, result.appointmentId ?? null);
    } catch (error) {
        showFeedback(error instanceof Error ? error.message : "No se pudo agregar la cita ficticia.", true);
    } finally {
        createAppointmentButton.disabled = false;
    }
}

refreshButton.addEventListener("click", () => void loadTurns());
roomFilter.addEventListener("change", () => render(loadedTurns));
detailTab.addEventListener("click", () => setActiveView("detail"));
orderedTab.addEventListener("click", () => setActiveView("ordered"));
createAppointmentForm.addEventListener("submit", event => void createFictitiousAppointment(event));
setActiveView(activeView);
void loadTurns();
window.setInterval(() => void loadTurns(false), 3_000);
