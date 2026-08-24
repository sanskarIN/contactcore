import { dotnet } from './_framework/dotnet.js';
import * as contactcoreStorage from './contactcore-storage.js';

if (typeof window === 'undefined') {
    throw new Error('ContactCore Browser must run in a browser context.');
}

globalThis.contactcoreStorage = contactcoreStorage;

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();
await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
