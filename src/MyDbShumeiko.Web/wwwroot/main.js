// Службовий завантажувач із шаблону Avalonia: запускає середовище .NET у браузері.
import { dotnet } from './_framework/dotnet.js'

// Буфер обміну. Avalonia працює з ним через navigator.clipboard, але браузер надає цей об'єкт
// лише захищеним сторінкам (HTTPS або localhost): на звичайній HTTP-адресі звернення до нього
// зупиняло весь застосунок. Тому вставлення робимо через подію paste, яка доступна завжди:
//   1) справжнє натискання Ctrl+V приховуємо від Avalonia, і браузер надсилає подію paste;
//   2) запам'ятовуємо текст із цієї події та повторюємо натискання вже для Avalonia;
//   3) Avalonia запитує текст у readText() і отримує запам'ятоване.
const nativeClipboard = navigator.clipboard;
let pastedText = '';
let pasteKey = null;
let replaying = false;

const isPasteShortcut = (e) =>
    ((e.ctrlKey || e.metaKey) && !e.altKey && e.code === 'KeyV') || (e.shiftKey && e.code === 'Insert');

window.addEventListener('keydown', (e) => {
    if (replaying || !isPasteShortcut(e))
        return;
    pasteKey = { ctrlKey: e.ctrlKey || e.code === 'Insert', metaKey: e.metaKey };
    e.stopImmediatePropagation();
}, true);

window.addEventListener('paste', (e) => {
    e.preventDefault();
    e.stopImmediatePropagation();
    if (!pasteKey)
        return;

    pastedText = (e.clipboardData?.getData('text/plain') ?? '').replace(/[\r\n]+/g, ' ');
    const key = { key: 'v', code: 'KeyV', bubbles: true, cancelable: true, ...pasteKey };
    pasteKey = null;
    replaying = true;
    try {
        e.target.dispatchEvent(new KeyboardEvent('keydown', key));
    } finally {
        replaying = false;
    }
    // Текст потрібен лише для цього одного вставлення.
    setTimeout(() => { pastedText = ''; }, 500);
}, true);

Object.defineProperty(navigator, 'clipboard', {
    value: {
        readText: () => Promise.resolve(pastedText),
        writeText(text) {
            // Відмова браузера (наприклад, без дозволу) не повинна зупиняти застосунок.
            if (nativeClipboard)
                return nativeClipboard.writeText(text).catch(() => { });

            // Без navigator.clipboard кладемо текст у буфер через подію copy.
            const put = (e) => {
                e.clipboardData.setData('text/plain', text);
                e.preventDefault();
                e.stopImmediatePropagation();
            };
            window.addEventListener('copy', put, true);
            try {
                document.execCommand('copy');
            } finally {
                window.removeEventListener('copy', put, true);
            }
            return Promise.resolve();
        },
    },
});

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .create();

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
