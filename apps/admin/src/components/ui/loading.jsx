import { Loader2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';

export function Loading({ className, size = 'default', text }) {
  const { t } = useTranslation();
  const sizeClasses = {
    sm: 'h-4 w-4',
    default: 'h-6 w-6',
    lg: 'h-8 w-8',
  };
  const resolvedText = text ?? t('common.loading');

  return (
    <div className={`flex items-center justify-center space-x-2 ${className || ''}`}>
      <Loader2 className={`animate-spin ${sizeClasses[size]}`} />
      {resolvedText && <span className="text-sm text-gray-500">{resolvedText}</span>}
    </div>
  );
}

export function LoadingPage({ text, className }) {
  const { t } = useTranslation();

  return (
    <div className={`flex min-h-[16rem] items-center justify-center rounded-lg border border-dashed bg-muted/20 ${className || ''}`}>
      <Loading size="lg" text={text ?? t('common.loading')} />
    </div>
  );
}

export default Loading;
