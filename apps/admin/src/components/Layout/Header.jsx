import { Bell, LogOut, Settings, Sun, Moon, Globe, CheckCheck, Building2, ShieldCheck, ArrowLeftRight, Menu, Sparkles, AlertTriangle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/context/AuthContext';
import { useTheme } from '@/context/ThemeContext';
import { useNavigate, Link, useLocation } from 'react-router-dom';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Breadcrumb,
  BreadcrumbList,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useTranslation } from 'react-i18next';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import { notificacoesApi } from '@/lib/api';
import { formatDateTime } from '@/lib/formatters';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { AVATAR_EVENT, getProfileAvatar } from '@/lib/profileAvatar';

// Mapeamento de rotas para chaves de i18n
const routeLabelKeys = {
  '': 'header.dashboard',
  'pessoas': 'header.people',
  'aniversarios-campanha': 'header.aniversarios-campanha',
  'visitantes': 'header.visitors',
  'perfis': 'header.profiles',
  'configuracoes-mensagens': 'header.messageSettings',
  'mensagens-agendadas': 'header.scheduledMessages',
  'equipes': 'header.teams',
  'cargos': 'header.roles',
  'voluntarios': 'header.volunteers',
  'eventos': 'header.events',
  'inscricoes-eventos': 'header.eventRegistrations',
  'patrimonio': 'header.patrimony',
  'categorias': 'header.patrimonyCategories',
  'relatorio-geral': 'header.patrimonyReport',
  'doacoes': 'Doações',
  'finalidades': 'Finalidades',
  'destaques-site': 'header.siteHighlights',
  'categorias-noticias': 'header.newsCategories',
  'noticias': 'header.news',
  'contatos': 'header.contacts',
  'usuarios': 'header.users',
  'operacao': 'header.operations',
  'perfil': 'header.profile',
  'categorias-midias': 'header.mediaCategories',
  'galerias-fotos': 'header.photoGalleries',
  'novo': 'header.new',
  'editar': 'header.edit',
};

function generateBreadcrumbs(pathname, t, locationState) {
  const paths = pathname.split('/').filter(Boolean);
  const breadcrumbs = [{ label: t('header.dashboard'), path: '/' }];
  const breadcrumbLabels = locationState?.breadcrumbLabels || {};

  if (paths.length === 0) {
    return breadcrumbs;
  }

  let currentPath = '';
  paths.forEach((segment, index) => {
    currentPath += `/${segment}`;
    const isLast = index === paths.length - 1;

    const customLabel = breadcrumbLabels[currentPath];
    if (customLabel) {
      breadcrumbs.push({
        label: customLabel,
        path: isLast ? null : currentPath,
      });
      return;
    }

    if (/^\d+$/.test(segment)) {
      return;
    } else {
      const key = routeLabelKeys[segment];
      const label = key ? t(key) : segment.charAt(0).toUpperCase() + segment.slice(1);
      breadcrumbs.push({
        label,
        path: isLast ? null : currentPath,
      });
    }
  });

  return breadcrumbs;
}

export function Header({ onMenuClick }) {
  const {
    usuario,
    logout,
    isPlatformAdmin,
    currentTenant,
    homeTenant,
    availableTenants,
    atualizarTenantOperacional,
    voltarParaTenantOrigem,
    operandoTenantRemoto,
    isOperatingHomeTenant,
  } = useAuth();
  const { toggleTheme, isDark, isVerbo } = useTheme();
  const navigate = useNavigate();
  const location = useLocation();
  const { t, i18n } = useTranslation();
  const breadcrumbs = generateBreadcrumbs(location.pathname, t, location.state);
  const [notificacoes, setNotificacoes] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [avatarUrl, setAvatarUrl] = useState(null);
  const confirmDialog = useConfirmDialog();

  const handleChangeLanguage = (lng) => {
    i18n.changeLanguage(lng);
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const truncateText = (text, maxLength = 20) => {
    if (!text) return '';
    return text.length > maxLength ? `${text.substring(0, maxLength)}...` : text;
  };

  const getInitials = (name) => {
    if (!name) return 'U';
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase();
  };

  useEffect(() => {
    setAvatarUrl(getProfileAvatar(usuario));

    const handleAvatarUpdate = () => {
      setAvatarUrl(getProfileAvatar(usuario));
    };

    window.addEventListener(AVATAR_EVENT, handleAvatarUpdate);
    return () => window.removeEventListener(AVATAR_EVENT, handleAvatarUpdate);
  }, [usuario]);

  useEffect(() => {
    if (!usuario) return;

    const run = async () => {
      try {
        const [itemsRes, countRes] = await Promise.all([
          notificacoesApi.getMinhas({ limit: 5 }),
          notificacoesApi.getUnreadCount(),
        ]);

        setNotificacoes(itemsRes.data || []);
        setUnreadCount(countRes.data?.count || 0);
      } catch (error) {
        console.error(t('notifications.errorLoad'), error);
      }
    };

    run();
  }, [usuario, location.pathname, currentTenant?.id]);

  const applyTenantChange = async (tenantId) => {
    try {
      await atualizarTenantOperacional(tenantId);
      window.location.reload();
    } catch (error) {
      console.error(t('header.tenantSwitchErrorLog'), error);
      toast.error(t('header.tenantSwitchError'));
    }
  };

  const handleTenantChange = (value) => {
    const nextTenantId = Number(value);
    if (!nextTenantId || nextTenantId === currentTenant?.id) {
      return;
    }

    const nextTenant = availableTenants.find((tenant) => tenant.id === nextTenantId);
    if (!nextTenant) return;

    const isRemoteTarget = nextTenant.id !== homeTenant?.id;
    if (!isRemoteTarget) {
      applyTenantChange(nextTenantId);
      return;
    }

    confirmDialog.show({
      title: t('header.remoteTenantDialog.title'),
      description: t('header.remoteTenantDialog.description', {
        tenant: nextTenant.nomeExibicao || nextTenant.nome || nextTenant.slug,
      }),
      confirmText: t('header.remoteTenantDialog.confirm'),
      cancelText: t('actions.cancel'),
      onConfirm: async () => {
        await applyTenantChange(nextTenantId);
      },
    });
  };

  const handleReturnToHomeTenant = async () => {
    if (!homeTenant?.id || isOperatingHomeTenant) return;

    try {
      await voltarParaTenantOrigem();
      window.location.reload();
    } catch (error) {
      console.error(t('header.returnToHomeTenantErrorLog'), error);
      toast.error(t('header.returnToHomeTenantError'));
    }
  };

  const handleNotificationClick = async (notificacao) => {
    try {
      if (!notificacao.dataLeitura) {
        await notificacoesApi.marcarComoLida(notificacao.id);
      }
    } catch (error) {
      console.error('Erro ao marcar notificação como lida', error);
    } finally {
      setNotificacoes((current) =>
        current.map((item) => (item.id === notificacao.id ? { ...item, dataLeitura: item.dataLeitura || new Date().toISOString() } : item))
      );
      setUnreadCount((current) => (notificacao.dataLeitura ? current : Math.max(0, current - 1)));

      if (notificacao.link) {
        navigate(notificacao.link);
      }
    }
  };

  const handleMarkAllNotifications = async () => {
    try {
      await notificacoesApi.marcarTodasComoLidas();
      setNotificacoes((current) => current.map((item) => ({ ...item, dataLeitura: item.dataLeitura || new Date().toISOString() })));
      setUnreadCount(0);
    } catch (error) {
      console.error(t('notifications.markAllError'), error);
    }
  };

  return (
    <header className="border-b border-border bg-background">
      <div className="flex h-14 items-center justify-between gap-2 px-3 sm:px-4 md:h-16 md:px-6">
        <div className="flex min-w-0 flex-1 items-center gap-2 md:gap-4">
          <Button
            variant="ghost"
            size="icon"
            className="md:hidden"
            onClick={onMenuClick}
            aria-label={t('layout.openMenu', { defaultValue: 'Abrir menu' })}
          >
            <Menu className="h-5 w-5" />
          </Button>

          <h1 className="truncate text-base font-semibold text-foreground sm:text-lg md:text-xl">
            {t('app.name')}
          </h1>

          {breadcrumbs.length > 1 && (
            <Breadcrumb className="hidden md:flex">
              <BreadcrumbList>
                {breadcrumbs.map((crumb, index) => (
                  <div key={index} className="flex items-center">
                    {index > 0 && <BreadcrumbSeparator />}
                    <BreadcrumbItem>
                      {crumb.path ? (
                        <BreadcrumbLink asChild>
                          <Link to={crumb.path}>{crumb.label}</Link>
                        </BreadcrumbLink>
                      ) : (
                        <BreadcrumbPage>{crumb.label}</BreadcrumbPage>
                      )}
                    </BreadcrumbItem>
                  </div>
                ))}
              </BreadcrumbList>
            </Breadcrumb>
          )}
        </div>

        <div className="flex shrink-0 items-center gap-1 sm:gap-2">
          {isPlatformAdmin && availableTenants.length > 0 && (
            <div className="hidden h-10 items-center gap-2 rounded-full border border-border bg-card px-2.5 md:flex">
              <ShieldCheck className="h-4 w-4 shrink-0 text-primary" title={t('header.platformMode')} />
              {currentTenant?.logoUrl ? (
                <img
                  src={currentTenant.logoUrl}
                  alt={`Logo de ${currentTenant.nomeExibicao || currentTenant.nome || currentTenant.slug}`}
                  className="h-7 w-7 rounded-full border bg-white object-contain p-1"
                />
              ) : (
                <div className="flex h-7 w-7 items-center justify-center rounded-full border border-dashed text-muted-foreground">
                  <Building2 className="h-3.5 w-3.5" />
                </div>
              )}

              <div className="flex min-w-0 items-center gap-2">
                <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                  {t('header.operating')}
                </span>
                <span className="max-w-[150px] truncate text-sm font-semibold text-foreground">
                  {currentTenant?.nomeExibicao || currentTenant?.nome || currentTenant?.slug || t('header.selectTenant')}
                </span>
                {isOperatingHomeTenant ? (
                  <span className="rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide text-primary">
                    {t('header.origin')}
                  </span>
                ) : (
                  <span className="rounded-full bg-amber-500/10 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide text-amber-600 dark:text-amber-400">
                    {t('header.remote')}
                  </span>
                )}
              </div>

              <Select value={String(currentTenant?.id || '')} onValueChange={handleTenantChange}>
                <SelectTrigger className="h-8 min-w-[190px] rounded-full border-0 bg-transparent px-2 shadow-none focus-visible:ring-1">
                  <SelectValue placeholder={t('header.selectTenant')} />
                </SelectTrigger>
                <SelectContent>
                  {availableTenants.map((tenant) => (
                    <SelectItem key={tenant.id} value={String(tenant.id)}>
                      {tenant.nomeExibicao || tenant.nome || tenant.slug}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon">
              <Globe className="h-5 w-5" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-40">
            <DropdownMenuLabel>{t('language.label')}</DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={() => handleChangeLanguage('pt-BR')}>
              {t('language.ptBR')}
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => handleChangeLanguage('en-US')}>
              {t('language.enUS')}
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => handleChangeLanguage('es-ES')}>
              {t('language.esES')}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>

          <Tooltip>
            <TooltipTrigger asChild>
              <Button variant="ghost" size="icon" onClick={toggleTheme}>
                {isVerbo ? <Sparkles className="h-5 w-5" /> : isDark ? <Moon className="h-5 w-5" /> : <Sun className="h-5 w-5" />}
              </Button>
            </TooltipTrigger>
            <TooltipContent>
              {t('header.switchTheme', { defaultValue: 'Alternar tema' })} · {isVerbo ? 'WhatsFlow' : isDark ? t('header.useDarkTheme') : t('header.useLightTheme')}
            </TooltipContent>
          </Tooltip>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="relative">
                <Bell className="h-5 w-5" />
                {unreadCount > 0 && (
                  <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-bold text-white">
                    {unreadCount > 9 ? '9+' : unreadCount}
                  </span>
                )}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-[calc(100vw-1rem)] max-w-sm sm:w-96">
              <DropdownMenuLabel className="flex items-center justify-between gap-2">
                <span>{t('notifications.title')}</span>
                {unreadCount > 0 && (
                  <Button variant="ghost" size="sm" className="h-7 px-2 text-xs" onClick={handleMarkAllNotifications}>
                    <CheckCheck className="h-3.5 w-3.5 mr-1" />
                    {t('notifications.readAll')}
                  </Button>
                )}
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              {notificacoes.length === 0 ? (
                <div className="px-2 py-6 text-center text-sm text-muted-foreground">
                  {t('notifications.emptyShort')}
                </div>
              ) : (
                notificacoes.map((item) => (
                  <DropdownMenuItem
                    key={item.id}
                    className="items-start py-3"
                    onClick={() => handleNotificationClick(item)}
                  >
                    <div className="space-y-1">
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{item.titulo}</span>
                        {!item.dataLeitura && <span className="h-2 w-2 rounded-full bg-red-500" />}
                      </div>
                      <p className="max-w-[min(300px,calc(100vw-5rem))] text-xs text-muted-foreground whitespace-normal">
                        {item.mensagem}
                      </p>
                      <p className="text-[11px] text-muted-foreground">
                        {formatDateTime(item.dataCriacao)}
                      </p>
                    </div>
                  </DropdownMenuItem>
                ))
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => navigate('/notificacoes')}>
                {t('notifications.viewAll')}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          {usuario && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="flex items-center space-x-2">
                  <Avatar className="size-8">
                    <AvatarImage src={avatarUrl || undefined} alt={usuario.nome} />
                    <AvatarFallback className="text-xs font-semibold">
                      {getInitials(usuario.nome)}
                    </AvatarFallback>
                  </Avatar>
                  <span className="hidden md:inline max-w-[150px] truncate">
                    {truncateText(usuario.nome)}
                  </span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56">
                <DropdownMenuLabel>
                  <div className="flex flex-col space-y-1">
                    <p className="text-sm font-medium truncate">{usuario.nome}</p>
                    <p className="text-xs text-muted-foreground truncate">{usuario.email}</p>
                    {isPlatformAdmin && (
                      <div className="space-y-0.5">
                        <p className="text-[11px] text-muted-foreground">
                          {t('header.homeTenant')}: {homeTenant?.nome || homeTenant?.slug}
                        </p>
                        <p className="text-[11px] text-muted-foreground">
                          {t('header.currentTenant')}: {currentTenant?.nome || currentTenant?.slug}
                        </p>
                      </div>
                    )}
                  </div>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem asChild>
                  <Link to="/perfil" className="flex items-center">
                    <Settings className="h-4 w-4 mr-2" />
                    {t('userMenu.myProfile')}
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={handleLogout} className="text-red-600">
                  <LogOut className="h-4 w-4 mr-2" />
                  {t('userMenu.logout')}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      </div>

      {/* Alerta só quando operando uma igreja que NÃO é a sua (estado de risco). */}
      {operandoTenantRemoto && (
        <div className="border-t border-amber-300 bg-amber-50 px-3 py-2.5 sm:px-4 md:px-6 dark:border-amber-900/50 dark:bg-amber-950/30">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-center gap-2 text-sm text-amber-800 dark:text-amber-200">
              <AlertTriangle className="h-4 w-4 shrink-0" />
              <span>
                {t('header.remoteWarningPrefix', { defaultValue: 'Você está operando a igreja' })}{' '}
                <span className="font-semibold">
                  {currentTenant?.nomeExibicao || currentTenant?.nome || currentTenant?.slug || t('header.notAvailable')}
                </span>
                {t('header.remoteWarningSuffix', { defaultValue: ' — não é a sua.' })}
              </span>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={handleReturnToHomeTenant}
              className="shrink-0 border-amber-400 bg-background text-amber-800 hover:bg-amber-100 dark:text-amber-200 dark:hover:bg-amber-900/40"
            >
              <ArrowLeftRight className="h-3.5 w-3.5" />
              {t('header.returnToHomeTenant')}
            </Button>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={confirmDialog.open}
        onOpenChange={(open) => {
          if (!open) confirmDialog.hide();
        }}
        onConfirm={confirmDialog.handleConfirm}
        title={confirmDialog.config.title}
        description={confirmDialog.config.description}
        confirmText={confirmDialog.config.confirmText}
        cancelText={confirmDialog.config.cancelText}
        variant={confirmDialog.config.variant}
        loading={confirmDialog.loading}
      />
    </header>
  );
}
