import { AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';

export function ErrorMessage({
  message = 'Ocorreu um erro inesperado',
  onRetry,
  className,
}) {
  return (
    <div className={`bg-red-50 border border-red-200 rounded-lg p-4 ${className || ''}`}>
      <div className="flex items-center">
        <AlertCircle className="h-5 w-5 text-red-600 mr-2" />
        <span className="text-red-800">{message}</span>
        {onRetry && (
          <Button type="button" variant="ghost" size="sm" onClick={onRetry} className="ml-auto text-red-700 hover:text-red-800 hover:bg-red-100">
            <RefreshCw className="h-4 w-4 mr-1" />
            Tentar novamente
          </Button>
        )}
      </div>
    </div>
  );
}

export function ErrorPage({
  title = 'Erro ao carregar',
  message = 'Ocorreu um erro ao carregar a página',
  onRetry,
  className,
}) {
  return (
    <div className={`flex min-h-[16rem] items-center justify-center rounded-lg border border-dashed border-red-200 bg-red-50/70 ${className || ''}`}>
      <div className="max-w-md text-center space-y-4 p-6">
        <AlertCircle className="h-12 w-12 text-red-600 mx-auto" />
        <div>
          <h3 className="text-lg font-semibold text-foreground">{title}</h3>
          <p className="text-muted-foreground">{message}</p>
        </div>
        {onRetry && (
          <Button type="button" onClick={onRetry}>
            <RefreshCw className="h-4 w-4 mr-2" />
            Tentar novamente
          </Button>
        )}
      </div>
    </div>
  );
}

export default ErrorMessage;
