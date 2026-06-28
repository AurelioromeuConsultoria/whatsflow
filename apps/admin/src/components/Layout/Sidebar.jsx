import { Link, useLocation } from 'react-router-dom';
import { useRef, useState } from 'react';
import { 
  Home, 
  Users, 
  MessageSquare, 
  Calendar,
  Group,
  Briefcase,
  Handshake,
  CalendarDays,
  Gift,
  CalendarOff,
  ArrowRightLeft,
  ClipboardCheck,
  Star,
  Tag,
  Newspaper,
  Contact,
  ClipboardList,
  Globe,
  ChevronDown,
  ChevronRight,
  UserCog,
  Images,
  Folder,
  ChevronsUpDown,
  BarChart3,
  Baby,
  ActivitySquare,
  LogIn,
  Cog,
  Shield,
  Package,
  Activity,
  Building2,
  CreditCard,
  HeartHandshake,
  PanelLeftClose,
  PanelLeftOpen
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES } from '@/utils/permissions';
import { useTranslation } from 'react-i18next';

const menuItems = [
  {
    titleKey: 'menu.dashboard',
    href: '/',
    icon: Home,
    permission: RESOURCES.DASHBOARD,
  },
  {
    titleKey: 'menu.contacts',
    href: '/contatos',
    icon: Contact,
    permission: RESOURCES.CONTATOS,
  },
  {
    titleKey: 'menu.tags',
    href: '/tags',
    icon: Tag,
    permission: RESOURCES.CONTATOS,
  },
  {
    title: 'Assinatura',
    href: '/billing',
    icon: CreditCard,
  },
  {
    title: 'Assinaturas (plataforma)',
    href: '/admin/assinaturas',
    icon: CreditCard,
    platformOnly: true,
  },
];

const menuGroups = [
  {
    titleKey: 'menu.communication',
    icon: MessageSquare,
    items: [
      {
        titleKey: 'menu.communicationCampaigns',
        href: '/comunicacao/campanhas',
        icon: MessageSquare,
        permission: RESOURCES.COMUNICACAO,
      },
      {
        titleKey: 'menu.communicationTemplates',
        href: '/comunicacao/templates',
        icon: Folder,
        permission: RESOURCES.COMUNICACAO,
      },
      {
        titleKey: 'menu.communicationSegments',
        href: '/comunicacao/segmentos',
        icon: Users,
        permission: RESOURCES.COMUNICACAO,
      },
      {
        titleKey: 'menu.scheduledMessages',
        href: '/mensagens-agendadas',
        icon: CalendarDays,
        permission: RESOURCES.MENSAGENS_AGENDADAS,
      },
      {
        titleKey: 'menu.messageLogs',
        href: '/comunicacao/logs',
        icon: ClipboardList,
        permission: RESOURCES.COMUNICACAO,
      },
    ],
  },
  {
    titleKey: 'menu.messageSettings',
    icon: Cog,
    items: [
      {
        titleKey: 'menu.whatsappAccounts',
        href: '/whatsapp/contas',
        icon: MessageSquare,
        permission: RESOURCES.CONFIG_MENSAGENS,
      },
      {
        titleKey: 'menu.messageSettings',
        href: '/configuracoes-mensagens',
        icon: Cog,
        permission: RESOURCES.CONFIG_MENSAGENS,
      },
    ],
  },
  {
    titleKey: 'menu.administration',
    icon: Shield,
    items: [
      {
        titleKey: 'menu.users',
        href: '/usuarios',
        icon: UserCog,
        permission: RESOURCES.USUARIOS,
        adminOnly: true,
      },
      {
        titleKey: 'menu.accessProfiles',
        href: '/perfis-acesso',
        icon: UserCog,
        permission: RESOURCES.PERFIS_ACESSO,
        adminOnly: true,
      },
      {
        titleKey: 'menu.audit',
        href: '/auditoria',
        icon: Shield,
        permission: RESOURCES.AUDITORIA,
        adminOnly: true,
      },
      {
        titleKey: 'menu.operations',
        href: '/operacao',
        icon: Activity,
        permission: RESOURCES.AUDITORIA,
        adminOnly: true,
      },
    ],
  },
  {
    titleKey: 'menu.platform',
    icon: Building2,
    items: [
      {
        titleKey: 'menu.tenants',
        href: '/plataforma/tenants',
        icon: Building2,
        platformOnly: true,
      },
    ],
  },
];

export function Sidebar({
  className,
  forceExpanded = false,
  showCollapseControl = true,
  onNavigate,
}) {
  const location = useLocation();
  const { can, isAdmin, isPlatformAdmin, currentTenant, homeTenant, operandoTenantRemoto } = useAuth();
  const { t } = useTranslation();
  const [isCollapsed, setIsCollapsed] = useState(() => {
    if (typeof window === 'undefined') return false;
    return window.localStorage.getItem('admin-sidebar-collapsed') === 'true';
  });
  const [isHoverExpanded, setIsHoverExpanded] = useState(false);
  const hoverOpenTimerRef = useRef(null);
  const isIconOnly = !forceExpanded && isCollapsed && !isHoverExpanded;

  const canSeeMenuItem = (item) => {
    if (item.platformOnly) {
      return isPlatformAdmin;
    }

    if (item.adminOnly) {
      return isAdmin;
    }

    return !item.permission || can(item.permission, 'view');
  };
  const [openGroups, setOpenGroups] = useState({});

  const toggleGroup = (groupKey) => {
    if (isIconOnly) {
      setIsCollapsed(false);
      setIsHoverExpanded(false);
      if (typeof window !== 'undefined') {
        window.localStorage.setItem('admin-sidebar-collapsed', 'false');
      }
      setOpenGroups((prev) => ({
        ...prev,
        [groupKey]: true,
      }));
      return;
    }

    setOpenGroups((prev) => ({
      ...prev,
      [groupKey]: !prev[groupKey],
    }));
  };

  const toggleSidebar = () => {
    if (hoverOpenTimerRef.current) {
      window.clearTimeout(hoverOpenTimerRef.current);
      hoverOpenTimerRef.current = null;
    }

    setIsCollapsed((current) => {
      const next = !current;
      setIsHoverExpanded(false);
      if (typeof window !== 'undefined') {
        window.localStorage.setItem('admin-sidebar-collapsed', String(next));
      }
      return next;
    });
  };

  const expandAllGroups = () => {
    const allGroups = menuGroups.reduce((acc, group) => {
      const groupKey = group.titleKey.split('.').pop();
      acc[groupKey] = true;
      return acc;
    }, {});
    setOpenGroups(allGroups);
  };

  const collapseAllGroups = () => {
    const allGroups = menuGroups.reduce((acc, group) => {
      const groupKey = group.titleKey.split('.').pop();
      acc[groupKey] = false;
      return acc;
    }, {});
    setOpenGroups(allGroups);
  };

  const allExpanded = menuGroups.every(group => {
    const groupKey = group.titleKey.split('.').pop();
    return openGroups[groupKey];
  });

  const toggleAllGroups = () => {
    if (allExpanded) {
      collapseAllGroups();
    } else {
      expandAllGroups();
    }
  };

  const isGroupActive = (items) => {
    return items.some((item) => {
      const isActive = location.pathname === item.href || 
        (item.href !== '/' && location.pathname.startsWith(item.href));
      return isActive;
    });
  };

  return (
    <div
      onMouseEnter={() => {
        if (forceExpanded) return;
        if (!isCollapsed || hoverOpenTimerRef.current) return;

        hoverOpenTimerRef.current = window.setTimeout(() => {
          setIsHoverExpanded(true);
          hoverOpenTimerRef.current = null;
        }, 180);
      }}
      onMouseLeave={() => {
        if (forceExpanded) return;
        if (hoverOpenTimerRef.current) {
          window.clearTimeout(hoverOpenTimerRef.current);
          hoverOpenTimerRef.current = null;
        }
        setIsHoverExpanded(false);
      }}
      onFocus={() => {
        if (!forceExpanded && isCollapsed) setIsHoverExpanded(true);
      }}
      className={cn(
        'flex h-dvh shrink-0 flex-col bg-sidebar border-r border-sidebar-border transition-[width] duration-350 ease-out',
        isIconOnly ? 'w-20' : 'w-64',
        className
      )}
    >
      {/* Logo */}
      <div className={cn(
        'relative flex h-16 items-center justify-center border-b border-sidebar-border px-3'
      )}>
        <div className="flex items-center min-w-0 gap-2">
          <span
            aria-label={t('app.name')}
            className={cn('flex shrink-0 items-center justify-center', isIconOnly ? 'h-9 w-9 text-2xl' : 'h-10 w-10 text-3xl')}
          >
            💬
          </span>
          {!isIconOnly && (
            <span className="truncate text-xl font-semibold text-sidebar-foreground">
              {t('app.name')}
            </span>
          )}
        </div>
        {showCollapseControl && (
          <Button
            variant="ghost"
            size="sm"
            onClick={toggleSidebar}
            className={cn('h-8 w-8 p-0 text-sidebar-foreground/60 hover:text-sidebar-foreground', !isIconOnly && 'absolute right-2 top-1/2 -translate-y-1/2')}
            title={isCollapsed ? t('layout.expandSidebar') : t('layout.collapseSidebar')}
            aria-label={isCollapsed ? t('layout.expandSidebar') : t('layout.collapseSidebar')}
          >
            {isCollapsed ? <PanelLeftOpen className="h-4 w-4" /> : <PanelLeftClose className="h-4 w-4" />}
          </Button>
        )}
      </div>

      {/* Navigation */}
      <nav className={cn('flex-1 space-y-1 overflow-y-auto', isIconOnly ? 'p-3' : 'p-4')}>
        {!isIconOnly && (
          <Button
            variant="ghost"
            size="sm"
            onClick={toggleAllGroups}
            className="mb-2 h-8 w-full justify-start gap-2 px-3 text-sidebar-foreground/70 hover:text-sidebar-foreground"
            title={allExpanded ? t('layout.collapseAll') : t('layout.expandAll')}
          >
            <ChevronsUpDown className="h-4 w-4" />
            <span className="text-xs">{allExpanded ? t('layout.collapseAll') : t('layout.expandAll')}</span>
          </Button>
        )}

        {menuItems.filter(canSeeMenuItem).map((item) => {
          const Icon = item.icon;
          const isActive = location.pathname === item.href || 
            (item.href !== '/' && location.pathname.startsWith(item.href));

          return (
            <Link
              key={item.href}
              to={item.href}
              onClick={onNavigate}
              title={item.title ?? t(item.titleKey)}
              className={cn(
                'flex h-10 items-center rounded-lg text-sm font-medium transition-colors',
                isIconOnly ? 'justify-center px-0' : 'space-x-3 px-3',
                isActive
                  ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                  : 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
              )}
            >
              <Icon className="h-5 w-5 shrink-0" />
              <span className={cn('truncate', isIconOnly && 'sr-only')}>{item.title ?? t(item.titleKey)}</span>
            </Link>
          );
        })}

        {/* Menu Groups */}
        {menuGroups.map((group, groupIndex) => {
          const visibleItems = group.items.filter(canSeeMenuItem);
          if (visibleItems.length === 0) return null;
          const groupKey = group.titleKey.split('.').pop();
          const isOpen = openGroups[groupKey] ?? false;
          const isActiveGroup = isGroupActive(visibleItems);
          const GroupIcon = group.icon;

          return (
            <Collapsible
              key={groupIndex}
              open={isOpen}
              onOpenChange={() => toggleGroup(groupKey)}
              className="mt-2"
            >
              <CollapsibleTrigger
                title={t(group.titleKey)}
                className={cn(
                  'flex h-10 w-full items-center rounded-lg text-sm font-medium transition-colors',
                  isIconOnly ? 'justify-center px-0' : 'justify-between px-3',
                  isActiveGroup
                    ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                    : 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
                )}
              >
                <div className={cn('flex items-center min-w-0', isIconOnly ? 'justify-center' : 'space-x-3')}>
                  <GroupIcon className="h-5 w-5 shrink-0" />
                  <span className={cn('truncate', isIconOnly && 'sr-only')}>{t(group.titleKey)}</span>
                </div>
                {!isIconOnly && (
                  isOpen ? (
                    <ChevronDown className="h-4 w-4 shrink-0" />
                  ) : (
                    <ChevronRight className="h-4 w-4 shrink-0" />
                  )
                )}
              </CollapsibleTrigger>
              <CollapsibleContent className={cn('space-y-1 mt-1', isIconOnly && 'hidden')}>
                {visibleItems.map((item) => {
                  const ItemIcon = item.icon;
                  const isActive = location.pathname === item.href || 
                    (item.href !== '/' && location.pathname.startsWith(item.href));

                  return (
                    <Link
                      key={item.href}
                      to={item.href}
                      onClick={onNavigate}
                      className={cn(
                        'ml-6 flex min-h-10 items-start space-x-3 rounded-lg px-3 py-2 text-sm font-medium leading-snug transition-colors',
                        isActive
                          ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                          : 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
                      )}
                      >
                        <ItemIcon className="mt-0.5 h-4 w-4 shrink-0" />
                      <span>{item.title ?? t(item.titleKey)}</span>
                    </Link>
                  );
                })}
              </CollapsibleContent>
            </Collapsible>
          );
        })}
      </nav>

      {/* Footer */}
      <div className={cn('border-t border-sidebar-border', isIconOnly ? 'p-3' : 'p-4')}>
        {isPlatformAdmin && !isIconOnly && (
          <div className={`mb-3 rounded-lg border px-3 py-3 text-xs ${operandoTenantRemoto ? 'border-amber-500/30 bg-amber-500/10 text-sidebar-foreground' : 'border-emerald-500/30 bg-emerald-500/10 text-sidebar-foreground'}`}>
            <div className="flex items-center gap-2 font-semibold">
              <Shield className="h-4 w-4" />
              <span>Modo plataforma</span>
            </div>
            <div className="mt-2 space-y-1 text-sidebar-foreground/80">
              <div>
                Origem: <span className="font-medium text-sidebar-foreground">{homeTenant?.slug || 'tenant-indefinido'}</span>
              </div>
              <div>
                Operando: <span className="font-medium text-sidebar-foreground">{currentTenant?.slug || 'tenant-indefinido'}</span>
              </div>
              <div className="pt-1 text-[11px] uppercase tracking-wide">
                {operandoTenantRemoto ? 'Tenant remoto ativo' : 'Tenant de origem ativo'}
              </div>
            </div>
          </div>
        )}

        <div className={cn('text-xs text-sidebar-foreground/60', isIconOnly && 'sr-only')}>
          {t('app.tagline')}
        </div>
        <div className={cn(
          'mt-3 rounded-lg border border-sidebar-border/70 bg-sidebar-accent/40 px-3 py-2 text-xs text-sidebar-foreground/80',
          isIconOnly && 'hidden'
        )}>
          <div className="font-medium">{currentTenant?.slug || 'tenant-indefinido'}</div>
          <div>{isPlatformAdmin ? 'Backoffice da plataforma' : 'Workspace atual'}</div>
        </div>
        <a
          href="https://malachdigital.com.br/"
          target="_blank"
          rel="noreferrer"
          title="Malach Digital"
          className={cn(
            'mt-3 flex items-center text-xs text-sidebar-foreground/70 hover:text-sidebar-foreground',
            isIconOnly ? 'justify-center' : 'gap-2'
          )}
        >
          <svg
            viewBox="0 0 24 24"
            aria-hidden="true"
            className="h-4 w-4 text-black dark:text-white"
            fill="currentColor"
          >
            <path d="M3 20V4h4l5 5 5-5h4v16h-4V10l-5 5-5-5v10H3z" />
          </svg>
          <span className={cn(isIconOnly && 'sr-only')}>{t('app.developedBy')}</span>
        </a>
      </div>
    </div>
  );
}
