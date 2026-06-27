import { Outlet } from 'react-router-dom';
import { useState } from 'react';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { Toaster } from '@/components/ui/sonner';
import { GlobalSearchPalette } from '@/components/GlobalSearchPalette';
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/components/ui/sheet';

export function Layout() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  return (
    <div className="flex min-h-dvh bg-background">
      <Sidebar className="hidden md:flex" />

      <Sheet open={mobileMenuOpen} onOpenChange={setMobileMenuOpen}>
        <SheetContent side="left" className="w-[min(20rem,calc(100vw-2rem))] gap-0 p-0">
          <SheetTitle className="sr-only">Menu administrativo</SheetTitle>
          <SheetDescription className="sr-only">
            Navegação principal do painel administrativo.
          </SheetDescription>
          <Sidebar
            className="w-full border-r-0"
            forceExpanded
            showCollapseControl={false}
            onNavigate={() => setMobileMenuOpen(false)}
          />
        </SheetContent>
      </Sheet>

      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <Header onMenuClick={() => setMobileMenuOpen(true)} />
        <main className="min-w-0 flex-1 overflow-auto p-3 sm:p-4 md:p-6">
          <Outlet />
        </main>
      </div>
      <GlobalSearchPalette />
      <Toaster />
    </div>
  );
}
