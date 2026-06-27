import { ArrowUpDown, ArrowUp, ArrowDown } from 'lucide-react';
import { TableHead } from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

/**
 * Componente de cabeçalho de tabela ordenável
 * 
 * @param {Object} props
 * @param {string} props.field - Campo para ordenação
 * @param {Function} props.onSort - Função de callback quando clicado
 * @param {Object} props.sortConfig - Configuração de ordenação atual: { field, direction }
 * @param {string} props.children - Conteúdo do cabeçalho
 * @param {string} props.className - Classes CSS adicionais
 */
export function SortableTableHeader({
  field,
  onSort,
  sortConfig = {},
  children,
  className,
}) {
  const isActive = sortConfig.field === field;
  const isAsc = sortConfig.direction === 'asc';

  const getIcon = () => {
    if (!isActive) {
      return <ArrowUpDown className="h-4 w-4 ml-1 text-muted-foreground opacity-50" />;
    }
    return isAsc 
      ? <ArrowUp className="h-4 w-4 ml-1 text-primary" />
      : <ArrowDown className="h-4 w-4 ml-1 text-primary" />;
  };

  return (
    <TableHead className={cn(className)}>
      <Button
        variant="ghost"
        size="sm"
        className="h-8 -ml-3 hover:bg-transparent"
        onClick={() => onSort(field)}
      >
        <span className="font-medium">{children}</span>
        {getIcon()}
      </Button>
    </TableHead>
  );
}
