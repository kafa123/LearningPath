import { loader } from '@monaco-editor/react'
import * as monaco from 'monaco-editor'
import EditorWorker from 'monaco-editor/editor/editor.worker?worker'

interface MonacoWorkerHost {
  MonacoEnvironment: {
    getWorker: (workerId: string, label: string) => Worker
  }
}

// The editor is bundled locally instead of loaded from a CDN so the lab keeps
// working without internet access. Only the base editor worker is required for
// syntax highlighting in C# and Python.
;(self as unknown as MonacoWorkerHost).MonacoEnvironment = {
  getWorker: () => new EditorWorker(),
}

monaco.editor.defineTheme('psel-lab-dark', {
  base: 'vs-dark',
  inherit: true,
  rules: [
    { token: 'comment', foreground: '64748b', fontStyle: 'italic' },
    { token: 'keyword', foreground: '6ee7b7' },
    { token: 'string', foreground: 'fcd34d' },
    { token: 'number', foreground: '7dd3fc' },
    { token: 'type', foreground: 'a5b4fc' },
    { token: 'delimiter', foreground: '94a3b8' },
  ],
  colors: {
    'editor.background': '#07101d',
    'editor.foreground': '#e2e8f0',
    'editorLineNumber.foreground': '#334155',
    'editorLineNumber.activeForeground': '#6ee7b7',
    'editor.selectionBackground': '#0f766e66',
    'editor.inactiveSelectionBackground': '#0f766e33',
    'editor.lineHighlightBackground': '#ffffff08',
    'editorCursor.foreground': '#34d399',
    'editorIndentGuide.background1': '#ffffff10',
    'editorIndentGuide.activeBackground1': '#34d39955',
    'editorWidget.background': '#0b1626',
    'editorGutter.background': '#07101d',
    'scrollbarSlider.background': '#ffffff14',
    'scrollbarSlider.hoverBackground': '#ffffff24',
  },
})

loader.config({ monaco })
