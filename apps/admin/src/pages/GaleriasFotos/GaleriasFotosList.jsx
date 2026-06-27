import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Filter, Image, Calendar, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState, PageRefreshButton } from '@/components/ui/page-state';
import { formatDate } from '@/lib/formatters';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { usePagination } from '@/hooks/usePagination';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { galeriasFotosApi, eventosApi, categoriasMidiasApi } from '@/lib/api';
import { UPLOADS_BASE_URL } from '@/lib/env';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function GaleriasFotosList() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [eventos, setEventos] = useState([]);
  const [categorias, setCategorias] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [busca, setBusca] = useState('');
  const [eventoFilter, setEventoFilter] = useState('');
  const [categoriaFilter, setCategoriaFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const confirmDialog = useConfirmDialog();

  const load = async ({ silent = false } = {}) => {
    try {
      if (silent) {
        setRefreshing(true);
      } else {
        setLoading(true);
      }
      if (!silent) {
        setError(null);
      }
      const [galeriasRes, eventosRes, categoriasRes] = await Promise.all([
        galeriasFotosApi.getAll(),
        eventosApi.getAll(),
        categoriasMidiasApi.getAll(),
      ]);
      setItems(galeriasRes.data || []);
      setEventos(eventosRes.data || []);
      setCategorias(categoriasRes.data || []);
    } catch (err) {
      setError(t('photoGalleries.errorLoad'));
      console.error(err);
    } finally {
      if (silent) {
        setRefreshing(false);
      } else {
        setLoading(false);
      }
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleDelete = async (id) => {
    const galeria = items.find(g => g.id === id);
    confirmDialog.show({
      title: t('photoGalleries.deleteTitle'),
      description: t('photoGalleries.deleteDescription', { name: galeria?.nome || t('photoGalleries.emptyMessage') }),
      confirmText: t('accessProfiles.permissionDelete'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await galeriasFotosApi.delete(id);
          toast.success(t('photoGalleries.deleteSuccess'));
          await load();
        } catch (err) {
          toast.error(t('photoGalleries.deleteError'));
          console.error(err);
          throw err;
        }
      },
    });
  };

  const filtered = items.filter((g) => {
    if (busca && !g.nome?.toLowerCase().includes(busca.toLowerCase()) && !g.descricao?.toLowerCase().includes(busca.toLowerCase())) return false;
    if (eventoFilter && String(g.eventoId) !== eventoFilter) return false;
    if (categoriaFilter && String(g.categoriaMidiaId) !== categoriaFilter) return false;
    if (statusFilter !== '' && String(g.ativo) !== statusFilter) return false;
    return true;
  });

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(filtered, 12);

  const getImagemUrl = (caminho) => {
    if (!caminho) return null;
    // Normalizar o caminho (remover barra inicial se existir para evitar duplicação)
    const caminhoNormalizado = caminho.startsWith('/') ? caminho.substring(1) : caminho;
    return `${UPLOADS_BASE_URL}/${caminhoNormalizado}`;
  };

  if (loading) return <LoadingPage text={t('photoGalleries.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('photoGalleries.title')}</h1>
          <p className="text-muted-foreground">{t('photoGalleries.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <PageRefreshButton onClick={() => load({ silent: true })} refreshing={refreshing} />
          <Button asChild>
            <Link to="/galerias-fotos/novo">
              <Plus className="h-4 w-4 mr-2" /> {t('photoGalleries.new')}
            </Link>
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('photoGalleries.filters')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('photoGalleries.search')}</label>
              <Input
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
                placeholder={t('photoGalleries.searchPlaceholder')}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('photoGalleries.event')}</label>
              <Select value={eventoFilter || 'all'} onValueChange={(value) => setEventoFilter(value === 'all' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('photoGalleries.allEvents')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('photoGalleries.allEvents')}</SelectItem>
                  {eventos.map((e) => (
                    <SelectItem key={e.id} value={String(e.id)}>{e.titulo}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('photoGalleries.category')}</label>
              <Select value={categoriaFilter || 'all'} onValueChange={(value) => setCategoriaFilter(value === 'all' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('photoGalleries.allCategories')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('photoGalleries.allCategories')}</SelectItem>
                  {categorias.map((c) => (
                    <SelectItem key={c.id} value={String(c.id)}>{c.nome}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('photoGalleries.status')}</label>
              <Select value={statusFilter || 'all'} onValueChange={(value) => setStatusFilter(value === 'all' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder={t('photoGalleries.allStatus')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('photoGalleries.allStatus')}</SelectItem>
                  <SelectItem value="true">{t('photoGalleries.active')}</SelectItem>
                  <SelectItem value="false">{t('photoGalleries.inactive')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {filtered.length === 0 ? (
        <Card>
          <CardContent className="py-8">
            <PageEmptyState
              title={t('photoGalleries.emptyMessage')}
              description="Ajuste os filtros ou crie uma nova galeria."
              action={(
                <Button asChild>
                  <Link to="/galerias-fotos/novo">
                    <Plus className="h-4 w-4 mr-2" /> {t('photoGalleries.new')}
                  </Link>
                </Button>
              )}
            />
          </CardContent>
        </Card>
      ) : (
        <>
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {paginatedItems.map((galeria) => {
            const imagemUrl = getImagemUrl(galeria.imagemDestaque);
            return (
              <Card key={galeria.id} className="overflow-hidden">
                <div className="relative h-48 bg-gray-100">
                  {imagemUrl ? (
                    <img
                      src={imagemUrl}
                      alt={galeria.nome}
                      className="w-full h-full object-cover"
                      onError={(e) => {
                        e.target.style.display = 'none';
                        e.target.nextSibling.style.display = 'flex';
                      }}
                    />
                  ) : null}
                  <div className={`absolute inset-0 flex items-center justify-center ${imagemUrl ? 'hidden' : ''}`}>
                    <Image className="h-16 w-16 text-gray-400" />
                  </div>
                  {!galeria.ativo && (
                    <div className="absolute top-2 right-2">
                      <Badge variant="destructive">{t('photoGalleries.inactive')}</Badge>
                    </div>
                  )}
                </div>
                <CardContent className="p-4">
                  <h3 className="font-semibold text-lg mb-2">{galeria.nome}</h3>
                  <p className="text-sm text-muted-foreground mb-3 line-clamp-2">{galeria.descricao || t('photoGalleries.noDescription')}</p>
                  
                  <div className="space-y-2 mb-4 text-sm">
                    <div className="flex items-center gap-2">
                      <Calendar className="h-4 w-4 text-muted-foreground" />
                      <span>{formatDate(galeria.data)}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <Image className="h-4 w-4 text-muted-foreground" />
                      <span>{t('photoGalleries.photosCount', { count: galeria.quantidadeFotos || 0 })}</span>
                    </div>
                    {galeria.eventoTitulo && (
                      <div className="text-muted-foreground">{t('photoGalleries.eventLabel')}: {galeria.eventoTitulo}</div>
                    )}
                    {galeria.categoriaMidiaNome && (
                      <div className="text-muted-foreground">{t('photoGalleries.categoryLabel')}: {galeria.categoriaMidiaNome}</div>
                    )}
                  </div>

                  <div className="flex items-center space-x-2">
                    <Button variant="outline" size="sm" asChild className="flex-1">
                      <Link to={`/galerias-fotos/${galeria.id}/fotos`}>
                        <Image className="h-4 w-4 mr-2" /> {t('photoGalleries.viewPhotos')}
                      </Link>
                    </Button>
                    <Button variant="ghost" size="sm" asChild>
                      <Link to={`/galerias-fotos/${galeria.id}/editar`}>
                        <Edit className="h-4 w-4" />
                      </Link>
                    </Button>
                    <Button variant="ghost" size="sm" onClick={() => handleDelete(galeria.id)}>
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            );
          })}
          </div>
          <DataTablePagination
            page={page}
            pageSize={pageSize}
            total={total}
            onPageChange={setPage}
            onPageSizeChange={setPageSize}
          />
        </>
      )}

      <ConfirmDialog
        open={confirmDialog.open}
        onOpenChange={confirmDialog.hide}
        onConfirm={confirmDialog.handleConfirm}
        title={confirmDialog.config.title}
        description={confirmDialog.config.description}
        confirmText={confirmDialog.config.confirmText}
        cancelText={confirmDialog.config.cancelText}
        variant={confirmDialog.config.variant}
        loading={confirmDialog.loading}
      />
    </div>
  );
}
