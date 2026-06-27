import { useEffect, useRef, useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useTheme } from '@/context/ThemeContext';
import { authApi } from '@/lib/api';
import { primeiroErroSenha } from '@/lib/passwordPolicy';
import { PasswordRequirements } from '@/components/PasswordRequirements';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Slider } from '@/components/ui/slider';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { toast } from 'sonner';
import { User, Mail, Shield, Calendar, Clock, Lock, Palette, Sun, Moon, Sparkles, Check, Camera, Upload, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { formatDateTime } from '@/lib/formatters';
import { getProfileAvatar, removeProfileAvatar, setProfileAvatar } from '@/lib/profileAvatar';

const TIPO_USUARIO_COLORS = {
  1: 'bg-blue-100 text-blue-800',
  2: 'bg-green-100 text-green-800',
  3: 'bg-purple-100 text-purple-800',
};

const TIPO_USUARIO_KEYS = { 1: 'userTypeAdmin', 2: 'userTypePortal', 3: 'userTypeBoth' };
const AVATAR_CROP_SIZE = 280;
const AVATAR_OUTPUT_SIZE = 256;

const THEME_OPTIONS = [
  {
    value: 'light',
    labelKey: 'light',
    descriptionKey: 'themeLightDescription',
    Icon: Sun,
    swatches: ['#ffffff', '#f8fafc', '#111827'],
  },
  {
    value: 'dark',
    labelKey: 'dark',
    descriptionKey: 'themeDarkDescription',
    Icon: Moon,
    swatches: ['#171717', '#333333', '#f5f5f5'],
  },
  {
    value: 'verbo',
    labelKey: 'verboTheme',
    descriptionKey: 'themeVerboDescription',
    Icon: Sparkles,
    swatches: ['#7c3aed', '#2563eb', '#06b6d4'],
  },
];

export default function Perfil() {
  const { usuario: usuarioContext, atualizarUsuario } = useAuth();
  const { theme, setTheme } = useTheme();
  const { t } = useTranslation();
  const fileInputRef = useRef(null);
  const dragStateRef = useRef(null);
  const [usuario, setUsuario] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [avatarUrl, setAvatarUrl] = useState(null);
  const [avatarDialogOpen, setAvatarDialogOpen] = useState(false);
  const [avatarSource, setAvatarSource] = useState(null);
  const [avatarImage, setAvatarImage] = useState(null);
  const [avatarZoom, setAvatarZoom] = useState(1.1);
  const [avatarOffset, setAvatarOffset] = useState({ x: 0, y: 0 });
  const [senhaData, setSenhaData] = useState({
    senhaAtual: '',
    novaSenha: '',
    confirmarSenha: '',
  });
  const [alterandoSenha, setAlterandoSenha] = useState(false);

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await authApi.me();
      setUsuario(res.data);
      atualizarUsuario(res.data);
      setAvatarUrl(getProfileAvatar(res.data));
    } catch (err) {
      setError(t('profile.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

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

  const handleSelectAvatarFile = (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      toast.error(t('profile.avatarInvalidFile'));
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const result = String(reader.result || '');
      const image = new Image();
      image.onload = () => {
        setAvatarSource(result);
        setAvatarImage(image);
        setAvatarZoom(1.1);
        setAvatarOffset({ x: 0, y: 0 });
        setAvatarDialogOpen(true);
      };
      image.onerror = () => toast.error(t('profile.avatarLoadError'));
      image.src = result;
    };
    reader.readAsDataURL(file);
    event.target.value = '';
  };

  const getAvatarRenderMetrics = () => {
    if (!avatarImage) return null;
    const baseScale = Math.max(AVATAR_CROP_SIZE / avatarImage.width, AVATAR_CROP_SIZE / avatarImage.height);
    const width = avatarImage.width * baseScale * avatarZoom;
    const height = avatarImage.height * baseScale * avatarZoom;
    return {
      width,
      height,
      left: (AVATAR_CROP_SIZE - width) / 2 + avatarOffset.x,
      top: (AVATAR_CROP_SIZE - height) / 2 + avatarOffset.y,
    };
  };

  const handleAvatarPointerDown = (event) => {
    if (!avatarImage) return;
    event.currentTarget.setPointerCapture(event.pointerId);
    dragStateRef.current = {
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
      offset: avatarOffset,
    };
  };

  const handleAvatarPointerMove = (event) => {
    const dragState = dragStateRef.current;
    if (!dragState || dragState.pointerId !== event.pointerId) return;

    setAvatarOffset({
      x: dragState.offset.x + event.clientX - dragState.startX,
      y: dragState.offset.y + event.clientY - dragState.startY,
    });
  };

  const handleAvatarPointerUp = (event) => {
    if (dragStateRef.current?.pointerId === event.pointerId) {
      dragStateRef.current = null;
    }
  };

  const handleSaveAvatar = () => {
    const metrics = getAvatarRenderMetrics();
    if (!avatarImage || !metrics || !usuario) return;

    const canvas = document.createElement('canvas');
    canvas.width = AVATAR_OUTPUT_SIZE;
    canvas.height = AVATAR_OUTPUT_SIZE;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const ratio = AVATAR_OUTPUT_SIZE / AVATAR_CROP_SIZE;
    ctx.clearRect(0, 0, AVATAR_OUTPUT_SIZE, AVATAR_OUTPUT_SIZE);
    ctx.save();
    ctx.beginPath();
    ctx.arc(AVATAR_OUTPUT_SIZE / 2, AVATAR_OUTPUT_SIZE / 2, AVATAR_OUTPUT_SIZE / 2, 0, Math.PI * 2);
    ctx.clip();
    ctx.drawImage(
      avatarImage,
      metrics.left * ratio,
      metrics.top * ratio,
      metrics.width * ratio,
      metrics.height * ratio,
    );
    ctx.restore();

    const dataUrl = canvas.toDataURL('image/png');
    setProfileAvatar(usuario, dataUrl);
    setAvatarUrl(dataUrl);
    setAvatarDialogOpen(false);
    toast.success(t('profile.avatarSaved'));
  };

  const handleRemoveAvatar = () => {
    removeProfileAvatar(usuario);
    setAvatarUrl(null);
    toast.success(t('profile.avatarRemoved'));
  };

  const avatarMetrics = getAvatarRenderMetrics();

  const handleAlterarSenha = async (e) => {
    e.preventDefault();

    if (!senhaData.senhaAtual || !senhaData.novaSenha) {
      toast.error(t('profile.fillAllFields'));
      return;
    }

    const erroSenha = primeiroErroSenha(senhaData.novaSenha);
    if (erroSenha) {
      toast.error(erroSenha);
      return;
    }

    if (senhaData.novaSenha !== senhaData.confirmarSenha) {
      toast.error(t('profile.passwordsDontMatch'));
      return;
    }

    try {
      setAlterandoSenha(true);
      await authApi.alterarSenha({
        senhaAtual: senhaData.senhaAtual,
        novaSenha: senhaData.novaSenha,
      });
      toast.success(t('profile.passwordChangeSuccess'));
      setSenhaData({
        senhaAtual: '',
        novaSenha: '',
        confirmarSenha: '',
      });
    } catch (err) {
      const errorMessage = err.response?.data?.message || t('profile.passwordChangeError');
      toast.error(errorMessage);
    } finally {
      setAlterandoSenha(false);
    }
  };

  if (loading) return <LoadingPage text={t('profile.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;
  if (!usuario) return <div>{t('profile.userNotFound')}</div>;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('profile.title')}</h1>
        <p className="text-muted-foreground">{t('profile.subtitle')}</p>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Camera className="h-5 w-5" />
              {t('profile.avatarTitle')}
            </CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-5 sm:flex-row sm:items-center">
            <Avatar className="size-24 border shadow-sm">
              <AvatarImage src={avatarUrl || undefined} alt={usuario.nome} />
              <AvatarFallback className="text-2xl font-semibold">
                {getInitials(usuario.nome)}
              </AvatarFallback>
            </Avatar>
            <div className="flex-1 space-y-3">
              <div>
                <p className="font-medium">{t('profile.avatarSubtitle')}</p>
                <p className="text-sm text-muted-foreground">{t('profile.avatarHint')}</p>
              </div>
              <div className="flex flex-wrap gap-2">
                <Button type="button" onClick={() => fileInputRef.current?.click()}>
                  <Upload className="h-4 w-4" />
                  {t('profile.avatarChoose')}
                </Button>
                {avatarUrl && (
                  <Button type="button" variant="outline" onClick={handleRemoveAvatar}>
                    <Trash2 className="h-4 w-4" />
                    {t('profile.avatarRemove')}
                  </Button>
                )}
              </div>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                className="hidden"
                onChange={handleSelectAvatarFile}
              />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Palette className="h-5 w-5" />
              {t('profile.preferences')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm font-medium text-muted-foreground mb-2 block">
                {t('profile.theme')}
              </label>
              <div className="grid gap-3 sm:grid-cols-3">
                {THEME_OPTIONS.map(({ value, labelKey, descriptionKey, Icon, swatches }) => {
                  const selected = theme === value;
                  return (
                    <button
                      key={value}
                      type="button"
                      onClick={() => setTheme(value)}
                      className={`relative rounded-lg border p-3 text-left transition hover:border-primary/50 hover:bg-accent/60 ${
                        selected ? 'border-primary bg-primary/5 ring-2 ring-primary/20' : 'border-border bg-background'
                      }`}
                      aria-pressed={selected}
                    >
                      {selected && (
                        <span className="absolute right-2 top-2 inline-flex size-5 items-center justify-center rounded-full bg-primary text-primary-foreground">
                          <Check className="size-3" />
                        </span>
                      )}
                      <div className="mb-3 flex items-center gap-2">
                        <Icon className="size-4 text-primary" />
                        <span className="text-sm font-semibold">{t(`profile.${labelKey}`)}</span>
                      </div>
                      <div className="mb-3 flex overflow-hidden rounded-md border">
                        {swatches.map((color) => (
                          <span key={color} className="h-8 flex-1" style={{ backgroundColor: color }} />
                        ))}
                      </div>
                      <p className="text-xs leading-5 text-muted-foreground">{t(`profile.${descriptionKey}`)}</p>
                    </button>
                  );
                })}
              </div>
              <p className="text-xs text-muted-foreground mt-2">
                {t('profile.themeHint')}
              </p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <User className="h-5 w-5" />
              {t('profile.personalData')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <User className="h-4 w-4" />
                {t('profile.name')}
              </label>
              <p className="text-base font-medium mt-1">{usuario.nome}</p>
            </div>
            <div>
              <label className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <Mail className="h-4 w-4" />
                {t('profile.email')}
              </label>
              <p className="text-base mt-1">{usuario.email}</p>
            </div>
            <div>
              <label className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <Shield className="h-4 w-4" />
                {t('profile.userType')}
              </label>
              <div className="mt-1">
                <span className={`px-3 py-1 rounded text-sm font-medium ${TIPO_USUARIO_COLORS[usuario.tipoUsuario] || 'bg-gray-100 text-gray-800'}`}>
                  {(TIPO_USUARIO_KEYS[usuario.tipoUsuario] && t('profile.' + TIPO_USUARIO_KEYS[usuario.tipoUsuario])) || usuario.tipoUsuarioDescricao}
                </span>
              </div>
            </div>
            <div>
              <label className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <Calendar className="h-4 w-4" />
                {t('profile.createdAt')}
              </label>
              <p className="text-base mt-1">
                {usuario.dataCriacao ? formatDateTime(usuario.dataCriacao) : '-'}
              </p>
            </div>
            {usuario.ultimoAcesso && (
              <div>
                <label className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                  <Clock className="h-4 w-4" />
                  {t('profile.lastAccess')}
                </label>
                <p className="text-base mt-1">
                  {formatDateTime(usuario.ultimoAcesso)}
                </p>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Lock className="h-5 w-5" />
              {t('profile.changePassword')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleAlterarSenha} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="senhaAtual">{t('profile.currentPassword')}</Label>
                <Input
                  id="senhaAtual"
                  name="senhaAtual"
                  type="password"
                  value={senhaData.senhaAtual}
                  onChange={(e) => setSenhaData((prev) => ({ ...prev, senhaAtual: e.target.value }))}
                  placeholder={t('profile.currentPasswordPlaceholder')}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="novaSenha">{t('profile.newPassword')}</Label>
                <Input
                  id="novaSenha"
                  name="novaSenha"
                  type="password"
                  value={senhaData.novaSenha}
                  onChange={(e) => setSenhaData((prev) => ({ ...prev, novaSenha: e.target.value }))}
                  placeholder={t('profile.newPasswordPlaceholder')}
                  required
                  minLength={8}
                />
                <PasswordRequirements senha={senhaData.novaSenha} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="confirmarSenha">{t('profile.confirmPassword')}</Label>
                <Input
                  id="confirmarSenha"
                  name="confirmarSenha"
                  type="password"
                  value={senhaData.confirmarSenha}
                  onChange={(e) => setSenhaData((prev) => ({ ...prev, confirmarSenha: e.target.value }))}
                  placeholder={t('profile.confirmPasswordPlaceholder')}
                  required
                />
              </div>
              <Button type="submit" disabled={alterandoSenha}>
                {alterandoSenha ? t('profile.changing') : t('profile.changePasswordButton')}
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>

      <Dialog open={avatarDialogOpen} onOpenChange={setAvatarDialogOpen}>
        <DialogContent className="sm:max-w-[520px]">
          <DialogHeader>
            <DialogTitle>{t('profile.avatarEditorTitle')}</DialogTitle>
            <DialogDescription>{t('profile.avatarEditorDescription')}</DialogDescription>
          </DialogHeader>

          <div className="space-y-5">
            <div className="flex justify-center">
              <div
                className="relative overflow-hidden rounded-full border-4 border-background bg-muted shadow-inner ring-1 ring-border"
                style={{ width: AVATAR_CROP_SIZE, height: AVATAR_CROP_SIZE }}
                onPointerDown={handleAvatarPointerDown}
                onPointerMove={handleAvatarPointerMove}
                onPointerUp={handleAvatarPointerUp}
                onPointerCancel={handleAvatarPointerUp}
              >
                {avatarSource && avatarMetrics && (
                  <img
                    src={avatarSource}
                    alt=""
                    draggable={false}
                    className="absolute max-w-none select-none"
                    style={{
                      width: avatarMetrics.width,
                      height: avatarMetrics.height,
                      left: avatarMetrics.left,
                      top: avatarMetrics.top,
                    }}
                  />
                )}
                <div className="pointer-events-none absolute inset-0 rounded-full ring-2 ring-white/80" />
              </div>
            </div>

            <div className="space-y-2">
              <div className="flex items-center justify-between text-sm">
                <Label>{t('profile.avatarZoom')}</Label>
                <span className="text-muted-foreground">{Math.round(avatarZoom * 100)}%</span>
              </div>
              <Slider
                value={[avatarZoom]}
                min={1}
                max={3}
                step={0.05}
                onValueChange={([value]) => setAvatarZoom(value)}
              />
              <p className="text-xs text-muted-foreground">{t('profile.avatarDragHint')}</p>
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setAvatarDialogOpen(false)}>
              {t('actions.cancel')}
            </Button>
            <Button type="button" onClick={handleSaveAvatar}>
              {t('profile.avatarSave')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}




