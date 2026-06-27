import { Button } from '@/components/ui/button';

export function TableRowActions({ children, className }) {
  return (
    <div className={`flex flex-wrap items-center justify-end gap-1 md:gap-2 ${className || ''}`}>
      {children}
    </div>
  );
}

export function RowIconLinkAction({ children, ...props }) {
  return (
    <Button variant="ghost" size="sm" asChild {...props}>
      {children}
    </Button>
  );
}

export function RowIconButtonAction(props) {
  return <Button variant="ghost" size="sm" {...props} />;
}
