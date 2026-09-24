const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const vm = require("node:vm");
const ts = require("typescript");

function element() {
    return {
        textContent: "",
        hidden: false,
        children: [],
        classList: { add() {}, remove() {}, toggle() {} },
        setAttribute() {},
        append(...children) { this.children.push(...children); },
        replaceChildren(...children) {
            this.children = children.flatMap(child => child.isFragment ? child.children : [child]);
        }
    };
}

function loadTelevision() {
    const elements = new Map();
    const intervals = new Map();
    let nextInterval = 0;
    const getElement = id => {
        if (!elements.has(id)) elements.set(id, element());
        return elements.get(id);
    };
    const document = {
        body: { dataset: { maxVisibleRows: "2", areaRotationSeconds: "8" } },
        getElementById: getElement,
        createElement: element,
        createDocumentFragment: () => ({ isFragment: true, children: [], append(...children) { this.children.push(...children); } })
    };
    const window = {
        setInterval(callback) { const id = ++nextInterval; intervals.set(id, callback); return id; },
        clearInterval(id) { intervals.delete(id); },
        addEventListener() {},
        speechSynthesis: { getVoices: () => [], addEventListener() {} }
    };
    const signalR = {
        LogLevel: { Warning: 2 },
        HubConnectionBuilder: class {
            withUrl() { return this; }
            withAutomaticReconnect() { return this; }
            configureLogging() { return this; }
            build() {
                return {
                    on() {}, onreconnecting() {}, onreconnected() {}, onclose() {},
                    start: () => new Promise(() => {})
                };
            }
        }
    };
    const storage = { getItem: () => null, setItem() {} };
    const context = vm.createContext({ document, window, signalR, localStorage: storage,
        sessionStorage: storage, console, Intl, setTimeout, requestAnimationFrame() {} });
    const source = fs.readFileSync(path.join(__dirname, "..", "src", "tv.ts"), "utf8");
    const js = ts.transpileModule(source, { compilerOptions: {
        target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.None
    } }).outputText;
    vm.runInContext(js, context);
    return { context, elements, intervals };
}

function patient(room, doctor, number) {
    return {
        publicId: `Paciente ${number}`, consultorio: room, medico: doctor,
        estado: "en-espera", priorityTier: 0, isPreferential: false,
        isMedicalExam: false, scheduledAt: null, arrivedAt: null,
        shouldAnnounce: false, isActiveCall: false
    };
}

test("divide consultorios reales sin duplicarlos y no cuenta páginas como consultorios", () => {
    const { context, elements } = loadTelevision();
    const items = [
        ...Array.from({ length: 5 }, (_, index) => patient("Cardiología", "Dr. Uno", index)),
        patient("Gastroenterología", "Dr. Dos", 6),
        patient("Odontología", "Dr. Tres", 7),
        patient("Cardiología", "Dr. Cuatro", 8)
    ];
    context.testItems = items;
    // Probar la distribución sin depender de la hora de ejecución del test.
    vm.runInContext("isInCurrentSession = () => true; restartAreaPanels(testItems)", context);
    const left = vm.runInContext("primaryAreaPanel.slides", context);
    const right = vm.runInContext("secondaryAreaPanel.slides", context);
    const leftRooms = new Set(left.map(slide => slide.consultorio));
    const rightRooms = new Set(right.map(slide => slide.consultorio));

    assert.equal(leftRooms.size, 2);
    assert.equal(rightRooms.size, 1);
    assert.deepEqual([...leftRooms].filter(room => rightRooms.has(room)), []);
    assert.equal(left.length, 5); // Tres páginas y dos médicos de Cardiología.
    assert.equal(elements.get("area-counter").textContent, "1 / 2");
    assert.equal(elements.get("area-counter-secondary").textContent, "1 / 1");
});

test("mantiene la página y su numeración cuando llega otro snapshot", () => {
    const { context, elements, intervals } = loadTelevision();
    context.testItems = [
        ...Array.from({ length: 5 }, (_, index) => patient("Cardiología", "Dr. Uno", index)),
        patient("Odontología", "Dr. Dos", 6)
    ];
    vm.runInContext("isInCurrentSession = () => true; restartAreaPanels(testItems)", context);
    const firstCounter = elements.get("area-counter").textContent;
    const timer = vm.runInContext("primaryAreaPanel.timer", context);
    intervals.get(timer)();
    const secondPageName = elements.get("area-list").children[0].children[0].children[0].textContent;

    vm.runInContext("restartAreaPanels(testItems)", context);
    assert.equal(firstCounter, "1 / 1");
    assert.equal(elements.get("area-counter").textContent, "1 / 1");
    assert.equal(elements.get("area-list").children[0].children[0].children[0].textContent, secondPageName);
    assert.equal(vm.runInContext("primaryAreaPanel.slideIndex", context), 1);
});
