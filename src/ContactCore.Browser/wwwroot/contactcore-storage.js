const databaseName = 'ContactCore';
const databaseVersion = 1;
const stateStore = 'state';
const contactsKey = 'contacts-v1';
const preferencesKey = 'contactcore.preferences.v1';
let preferencesFallback = '';

function requestAsPromise(request) {
    return new Promise((resolve, reject) => {
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error ?? new Error('IndexedDB request failed.'));
    });
}

function transactionAsPromise(transaction) {
    return new Promise((resolve, reject) => {
        transaction.oncomplete = () => resolve();
        transaction.onerror = () => reject(transaction.error ?? new Error('IndexedDB transaction failed.'));
        transaction.onabort = () => reject(transaction.error ?? new Error('IndexedDB transaction was aborted.'));
    });
}

function openDatabase() {
    return new Promise((resolve, reject) => {
        if (!globalThis.indexedDB) {
            reject(new Error('IndexedDB is unavailable in this browser context.'));
            return;
        }

        const request = globalThis.indexedDB.open(databaseName, databaseVersion);
        request.onupgradeneeded = () => {
            const database = request.result;
            if (!database.objectStoreNames.contains(stateStore)) {
                database.createObjectStore(stateStore);
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error ?? new Error('Could not open ContactCore IndexedDB storage.'));
        request.onblocked = () => reject(new Error('ContactCore storage upgrade is blocked by another browser tab.'));
    });
}

export async function loadContacts() {
    const database = await openDatabase();
    try {
        const transaction = database.transaction(stateStore, 'readonly');
        const completed = transactionAsPromise(transaction);
        const value = await requestAsPromise(transaction.objectStore(stateStore).get(contactsKey));
        await completed;
        return typeof value === 'string' ? value : '';
    } finally {
        database.close();
    }
}

export async function saveContacts(json) {
    if (typeof json !== 'string') throw new TypeError('ContactCore browser storage expects a JSON string.');

    const database = await openDatabase();
    try {
        const transaction = database.transaction(stateStore, 'readwrite');
        const completed = transactionAsPromise(transaction);
        transaction.objectStore(stateStore).put(json, contactsKey);
        await completed;
    } finally {
        database.close();
    }
}

export function loadPreferences() {
    try {
        return globalThis.localStorage?.getItem(preferencesKey) ?? preferencesFallback;
    } catch {
        return preferencesFallback;
    }
}

export function savePreferences(json) {
    preferencesFallback = typeof json === 'string' ? json : '';
    try {
        globalThis.localStorage?.setItem(preferencesKey, preferencesFallback);
    } catch {
        // Some privacy modes block localStorage. Keep a session-only fallback instead.
    }
}
