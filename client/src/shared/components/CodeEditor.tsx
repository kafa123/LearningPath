import Editor from '@monaco-editor/react'
import '@/shared/monaco/setupMonaco'

interface CodeEditorProps {
  filePath: string
  language: string
  value: string
  onChange: (value: string) => void
}

export function CodeEditor({ filePath, language, value, onChange }: CodeEditorProps) {
  return (
    <Editor
      height="100%"
      language={language}
      loading={
        <div className="grid h-full place-items-center text-xs text-slate-500">
          Loading editor...
        </div>
      }
      onChange={(nextValue) => onChange(nextValue ?? '')}
      options={{
        ariaLabel: `Code editor for ${filePath}`,
        automaticLayout: true,
        fontFamily:
          "'Cascadia Code', 'JetBrains Mono', 'Fira Code', ui-monospace, SFMono-Regular, monospace",
        fontSize: 13,
        lineHeight: 21,
        minimap: { enabled: false },
        padding: { top: 18, bottom: 18 },
        renderLineHighlight: 'gutter',
        scrollBeyondLastLine: false,
        smoothScrolling: true,
        tabSize: 4,
        wordWrap: 'on',
      }}
      path={`${language}/${filePath}`}
      theme="psel-lab-dark"
      value={value}
    />
  )
}
