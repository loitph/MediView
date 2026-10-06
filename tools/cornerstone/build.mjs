import { build } from 'esbuild';
import { copyFile, mkdir, rm } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { basename, dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const outdir = resolve(here, '../../src/Web/MediView.Web/wwwroot/lib/cornerstone');
const require = createRequire(import.meta.url);

const codecs = [
    '@cornerstonejs/codec-charls/decodewasm',
    '@cornerstonejs/codec-libjpeg-turbo-8bit/decodewasm',
    '@cornerstonejs/codec-libjxl/decodewasm',
    '@cornerstonejs/codec-openjpeg/decodewasm',
    '@cornerstonejs/codec-openjph/wasm',
];

const nodeBuiltinsAsBrowserStubs = {
    name: 'node-builtins-as-browser-stubs',
    setup(esbuild) {
        esbuild.onResolve({ filter: /^(crypto|events|fs|module|path|url|worker_threads)$/ }, args => ({
            path: args.path,
            namespace: 'node-stub',
        }));
        esbuild.onLoad({ filter: /.*/, namespace: 'node-stub' }, () => ({
            contents: 'export class EventEmitter {} export default {};',
            loader: 'js',
        }));
    },
};

await rm(outdir, { recursive: true, force: true });

await build({
    entryPoints: {
        cornerstone: resolve(here, 'cornerstone.js'),
        decodeImageFrameWorker: resolve(here, 'node_modules/@cornerstonejs/dicom-image-loader/dist/esm/decodeImageFrameWorker.js'),
    },
    bundle: true,
    format: 'esm',
    splitting: true,
    minify: true,
    legalComments: 'external',
    chunkNames: 'chunks/[name]-[hash]',
    outdir,
    plugins: [nodeBuiltinsAsBrowserStubs],
    logLevel: 'warning',
});

await mkdir(resolve(outdir, 'codecs'), { recursive: true });
for (const codec of codecs) {
    const wasm = require.resolve(codec);
    await copyFile(wasm, resolve(outdir, 'codecs', basename(wasm)));
}
