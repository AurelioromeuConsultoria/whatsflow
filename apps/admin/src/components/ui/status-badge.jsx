import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

const TONE_CLASSNAME = {
  neutral: 'bg-gray-100 text-gray-800 hover:bg-gray-100',
  info: 'bg-blue-100 text-blue-800 hover:bg-blue-100',
  success: 'bg-green-100 text-green-800 hover:bg-green-100',
  warning: 'bg-yellow-100 text-yellow-800 hover:bg-yellow-100',
  danger: 'bg-red-100 text-red-800 hover:bg-red-100',
};

export function StatusBadge({
  children,
  tone = 'neutral',
  className,
  ...props
}) {
  return (
    <Badge
      variant="secondary"
      className={cn(TONE_CLASSNAME[tone] || TONE_CLASSNAME.neutral, className)}
      {...props}
    >
      {children}
    </Badge>
  );
}

export function BooleanStatusBadge({
  value,
  trueLabel = 'Ativo',
  falseLabel = 'Inativo',
  trueTone = 'success',
  falseTone = 'neutral',
  ...props
}) {
  return (
    <StatusBadge tone={value ? trueTone : falseTone} {...props}>
      {value ? trueLabel : falseLabel}
    </StatusBadge>
  );
}
