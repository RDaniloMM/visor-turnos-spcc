import { copyFile } from "node:fs/promises";

await copyFile(
    new URL("../node_modules/@microsoft/signalr/dist/browser/signalr.min.js", import.meta.url),
    new URL("../../wwwroot/js/signalr.min.js", import.meta.url));
