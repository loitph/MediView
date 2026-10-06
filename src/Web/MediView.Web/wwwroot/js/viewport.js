import {
    initCore,
    initTools,
    initDicomImageLoader,
    RenderingEngine,
    Enums,
    addTool,
    ToolGroupManager,
    WindowLevelTool,
    PanTool,
    ZoomTool,
    StackScrollTool,
    ToolEnums,
} from '../lib/cornerstone/cornerstone.js';

const renderingEngineId = 'mediview';
const viewportId = 'mediview-stack';
const toolGroupId = 'mediview-tools';
const primaryTools = {
    windowLevel: WindowLevelTool.toolName,
    pan: PanTool.toolName,
    zoom: ZoomTool.toolName,
};
const allTools = [WindowLevelTool, PanTool, ZoomTool, StackScrollTool];

let accessToken;
let started = false;
let session;

function startCornerstone() {
    initCore();
    initTools();
    initDicomImageLoader({
        maxWebWorkers: 1,
        wasmBasePath: new URL('../lib/cornerstone/codecs/', import.meta.url).href,
        beforeSend: () => ({ Authorization: `Bearer ${accessToken}` }),
    });
    allTools.forEach(addTool);
    started = true;
}

export function init(element, token, listener) {
    accessToken = token;
    if (!started) {
        startCornerstone();
    }

    dispose();

    const engine = new RenderingEngine(renderingEngineId);
    engine.enableElement({ viewportId, type: Enums.ViewportType.STACK, element });

    const tools = ToolGroupManager.createToolGroup(toolGroupId);
    allTools.forEach(tool => tools.addTool(tool.toolName));
    tools.setToolActive(StackScrollTool.toolName, { bindings: [{ mouseButton: ToolEnums.MouseBindings.Wheel }] });
    tools.addViewport(viewportId, renderingEngineId);

    const onNewImage = event => listener.invokeMethodAsync('OnImageChanged', event.detail.imageIdIndex);
    const suppressMenu = event => event.preventDefault();
    element.addEventListener(Enums.Events.STACK_NEW_IMAGE, onNewImage);
    element.addEventListener('contextmenu', suppressMenu);

    const resizer = new ResizeObserver(() => engine.resize(true, true));
    resizer.observe(element);

    session = { engine, tools, element, onNewImage, suppressMenu, resizer };
    setTool('windowLevel');
}

export async function loadStack(urls) {
    const viewport = session.engine.getViewport(viewportId);
    const imageIds = urls.map(url => `wadouri:${new URL(url, document.baseURI).href}`);
    await viewport.setStack(imageIds, 0);
    viewport.render();
    return imageIds.length;
}

export function setTool(name) {
    Object.values(primaryTools).forEach(tool => session.tools.setToolPassive(tool));
    session.tools.setToolActive(primaryTools[name], { bindings: [{ mouseButton: ToolEnums.MouseBindings.Primary }] });
}

export function reset() {
    const viewport = session.engine.getViewport(viewportId);
    viewport.resetCamera();
    viewport.resetProperties();
    viewport.render();
}

export function dispose() {
    if (!session) {
        return;
    }

    session.element.removeEventListener(Enums.Events.STACK_NEW_IMAGE, session.onNewImage);
    session.element.removeEventListener('contextmenu', session.suppressMenu);
    session.resizer.disconnect();
    ToolGroupManager.destroyToolGroup(toolGroupId);
    session.engine.destroy();
    session = undefined;
}
