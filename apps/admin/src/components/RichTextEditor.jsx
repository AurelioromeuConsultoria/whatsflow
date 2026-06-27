import { useRef, useEffect, useState } from 'react';
import { Label } from '@/components/ui/label';
import { useTranslation } from 'react-i18next';

/**
 * Editor de texto rico simples que preserva formatação ao colar
 * Converte quebras de linha em parágrafos HTML
 */
export function RichTextEditor({ value, onChange, label, placeholder, required = false, name = 'texto' }) {
  const { t } = useTranslation();
  const textareaRef = useRef(null);
  const [isPasting, setIsPasting] = useState(false);

  useEffect(() => {
    const textarea = textareaRef.current;
    if (!textarea) return;

    const handlePaste = (e) => {
      e.preventDefault();
      setIsPasting(true);
      
      // Obter texto colado
      const pastedText = (e.clipboardData || window.clipboardData).getData('text/plain');
      
      // Obter posição do cursor
      const selectionStart = textarea.selectionStart;
      const selectionEnd = textarea.selectionEnd;
      const currentValue = value || '';
      
      // Converter quebras de linha duplas em parágrafos
      // Preservar quebras de linha simples como <br>
      const html = pastedText
        .split(/\n\s*\n/) // Dividir por linhas em branco (parágrafos)
        .map(paragraph => {
          const trimmed = paragraph.trim();
          if (!trimmed) return '';
          // Converter quebras de linha simples dentro do parágrafo em <br>
          return `<p>${trimmed.replace(/\n/g, '<br>')}</p>`;
        })
        .filter(p => p) // Remover parágrafos vazios
        .join('');

      // Construir novo valor
      const newValue = 
        currentValue.substring(0, selectionStart) + 
        html + 
        currentValue.substring(selectionEnd);
      
      // Atualizar valor
      onChange({ target: { name, value: newValue } });
      
      // Restaurar foco e posição do cursor após atualização
      setTimeout(() => {
        textarea.focus();
        const newPosition = selectionStart + html.length;
        textarea.setSelectionRange(newPosition, newPosition);
        setIsPasting(false);
      }, 10);
    };

    textarea.addEventListener('paste', handlePaste);
    
    return () => {
      textarea.removeEventListener('paste', handlePaste);
    };
  }, [value, onChange, name]);

  const handleChange = (e) => {
    if (!isPasting) {
      onChange(e);
    }
  };

  const fieldId = label?.toLowerCase().replace(/\s+/g, '-') || name;

  return (
    <div className="space-y-2">
      {label && (
        <Label htmlFor={fieldId}>
          {label} {required && <span className="text-red-500">*</span>}
        </Label>
      )}
      <div className="relative">
        <textarea
          ref={textareaRef}
          id={fieldId}
          name={name}
          value={value || ''}
          onChange={handleChange}
          placeholder={placeholder}
          required={required}
          className="w-full min-h-[300px] px-3 py-2 border rounded-md resize-y text-sm"
          style={{ whiteSpace: 'pre-wrap', fontFamily: 'inherit' }}
        />
        <div className="absolute bottom-2 right-2 text-xs text-muted-foreground bg-white px-2 py-1 rounded border">
          {t('richTextEditor.pasteHint')}
        </div>
      </div>
    </div>
  );
}
