import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { MessageSquareText } from 'lucide-react';

const INPUT_ID = 'prompt-dialog-input';

/**
 * Diálogo de entrada de texto, alinhado ao ConfirmDialog.
 * Substitui o window.prompt nativo por uma UI consistente e moderna.
 */
export function PromptDialog({ open, onOpenChange, value, onValueChange, onConfirm, config = {} }) {
  const {
    title,
    description,
    label,
    placeholder,
    confirmText,
    cancelText,
    required = false,
    multiline = true,
  } = config;

  const isInvalid = required && !String(value || '').trim();

  const handleConfirm = (event) => {
    if (isInvalid) {
      event.preventDefault();
      return;
    }
    onConfirm();
  };

  const handleKeyDown = (event) => {
    // Enter confirma em campos de linha única; em multiline, Ctrl/Cmd+Enter confirma.
    const submitCombo = multiline ? (event.key === 'Enter' && (event.metaKey || event.ctrlKey)) : event.key === 'Enter';
    if (submitCombo && !isInvalid) {
      event.preventDefault();
      onConfirm();
    }
  };

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <MessageSquareText className="h-5 w-5 text-primary" />
            <AlertDialogTitle>{title}</AlertDialogTitle>
          </div>
          {description ? (
            <AlertDialogDescription className="pt-2">{description}</AlertDialogDescription>
          ) : null}
        </AlertDialogHeader>

        <div className="space-y-2 py-1">
          {label ? <Label htmlFor={INPUT_ID}>{label}</Label> : null}
          {multiline ? (
            <Textarea
              id={INPUT_ID}
              value={value}
              onChange={(event) => onValueChange(event.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={placeholder}
              rows={4}
              autoFocus
            />
          ) : (
            <Input
              id={INPUT_ID}
              value={value}
              onChange={(event) => onValueChange(event.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={placeholder}
              autoFocus
            />
          )}
        </div>

        <AlertDialogFooter>
          <AlertDialogCancel>{cancelText || 'Cancelar'}</AlertDialogCancel>
          <AlertDialogAction onClick={handleConfirm} disabled={isInvalid}>
            {confirmText || 'Confirmar'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
