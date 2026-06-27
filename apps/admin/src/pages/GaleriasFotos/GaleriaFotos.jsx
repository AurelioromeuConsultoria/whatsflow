import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, Upload, Star, Eye, Image as ImageIcon, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { galeriasFotosApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { UPLOADS_BASE_URL } from '@/lib/env';
import { useTranslation } from 'react-i18next';

const FORMATOS_PERMITIDOS = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
const TAMANHO_MAXIMO = 10 * 1024 * 1024; // 10MB

export default function GaleriaFotos() {
  const { id } = useParams();
  const { t } = useTranslation();
  const [galeria, setGaleria] = useState(null);
  const [fotos, setFotos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [selectedFiles, setSelectedFiles] = useState([]);
  const [previewFiles, setPreviewFiles] = useState([]);
  const [viewingPhoto, setViewingPhoto] = useState(null);
  const [dragActive, setDragActive] = useState(false);
  const [imageErrors, setImageErrors] = useState(new Set());

  // Limpar erros de imagem ao trocar de galeria ou ao recarregar fotos
  useEffect(() => {
    setImageErrors(new Set());
  }, [id]);

  const load = async (isRefresh = false) => {
    try {
      // Só mostra tela de loading no carregamento inicial; refresh (ex.: após definir destaque) não pisca
      if (!isRefresh) setLoading(true);
      setError(null);
      if (!id) {
        setError(t('photoGalleries.invalidGalleryId'));
        return;
      }
      const res = await galeriasFotosApi.getById(id);
      if (res?.data) {
        setGaleria(res.data);
        await loadFotos(res.data);
      } else {
        setError(t('photoGalleries.galleryNotFound'));
      }
    } catch (err) {
      const errorMessage = err.response?.data?.message || err.message || t('photoGalleries.saveError');
      setError(errorMessage);
      console.error('Erro ao carregar galeria:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadFotos = async (galeriaData) => {
    if (!galeriaData?.caminhoDiretorio) {
      setFotos([]);
      return;
    }

    try {
      // Tentar buscar lista de fotos do endpoint
      const res = await galeriasFotosApi.listarFotos(id);
      if (res?.data && Array.isArray(res.data)) {
        setFotos(res.data);
        setImageErrors((prev) => new Set()); // reset erros ao recarregar lista
      } else {
        // Se o endpoint não retornar dados válidos, criar lista vazia
        setFotos([]);
      }
    } catch (err) {
      // Se o endpoint não existir (404), tentar uma solução alternativa
      if (err.response?.status === 404) {
        console.warn('Endpoint de listagem de fotos não disponível. Criando lista básica...');
        // Criar uma lista básica com a foto de destaque se existir
        const fotosList = [];
        if (galeriaData.imagemDestaque) {
          const nomeArquivo = galeriaData.imagemDestaque.split('/').pop();
          fotosList.push({ nomeArquivo, destaque: true });
        }
        setFotos(fotosList);
      } else {
        console.error('Erro ao carregar fotos:', err);
        setFotos([]);
      }
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]); // id é a única dependência necessária

  const validateFile = (file) => {
    if (!FORMATOS_PERMITIDOS.includes(file.type)) {
      toast.error(t('photoGalleries.formatNotSupported'));
      return false;
    }
    if (file.size > TAMANHO_MAXIMO) {
      toast.error(t('photoGalleries.fileTooBig'));
      return false;
    }
    return true;
  };

  const handleFileSelect = (files) => {
    const fileArray = Array.from(files);
    const validFiles = fileArray.filter(validateFile);
    
    if (validFiles.length !== fileArray.length) {
      return;
    }

    setSelectedFiles((prev) => [...prev, ...validFiles]);
    
    // Criar previews
    validFiles.forEach((file) => {
      const reader = new FileReader();
      reader.onload = (e) => {
        setPreviewFiles((prev) => [...prev, { file, preview: e.target.result }]);
      };
      reader.readAsDataURL(file);
    });
  };

  const handleDrag = (e) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  const handleDrop = (e) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      handleFileSelect(e.dataTransfer.files);
    }
  };

  const removeFile = (index) => {
    setSelectedFiles((prev) => prev.filter((_, i) => i !== index));
    setPreviewFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const handleUpload = async () => {
    if (selectedFiles.length === 0) {
      toast.error(t('photoGalleries.selectAtLeastOne'));
      return;
    }

    try {
      setUploading(true);
      const formData = new FormData();
      selectedFiles.forEach((file) => {
        formData.append('arquivos', file);
      });

      await galeriasFotosApi.upload(id, formData);
      toast.success(t('photoGalleries.uploadSuccess', { count: selectedFiles.length }));
      
      setSelectedFiles([]);
      setPreviewFiles([]);
      await load(true);
    } catch (err) {
      const errorMessage = err.response?.data?.message || t('photoGalleries.uploadError');
      toast.error(errorMessage);
      console.error(err);
    } finally {
      setUploading(false);
    }
  };

  const handleDefinirDestaque = async (nomeArquivo) => {
    try {
      await galeriasFotosApi.definirDestaque(id, nomeArquivo);
      toast.success(t('photoGalleries.featuredSetSuccess'));
      await load(true);
    } catch (err) {
      toast.error(t('photoGalleries.featuredSetError'));
      console.error(err);
    }
  };

  const getImagemUrl = (caminho) => {
    if (!caminho) return null;
    // O caminho já vem completo do backend (ex: uploads/fotos/...)
    // Normalizar o caminho (remover barra inicial se existir para evitar duplicação)
    let caminhoNormalizado = caminho.startsWith('/') ? caminho.substring(1) : caminho;
    // Garantir que não tenha barras duplas
    caminhoNormalizado = caminhoNormalizado.replace(/\/+/g, '/');
    const url = `${UPLOADS_BASE_URL}/${caminhoNormalizado}`;
    return url;
  };

  const getThumbnailUrl = (nomeArquivo) => {
    if (!galeria?.caminhoDiretorio || !nomeArquivo) return null;
    const dir = galeria.caminhoDiretorio.replace(/\\/g, '/').replace(/\/+/g, '/').replace(/^\//, '');
    const url = `${UPLOADS_BASE_URL}/${dir}/thumbnail/${nomeArquivo}`;
    return url;
  };

  const getOriginalUrl = (nomeArquivo) => {
    if (!galeria?.caminhoDiretorio || !nomeArquivo) return null;
    return `${UPLOADS_BASE_URL}/${galeria.caminhoDiretorio}/original/${nomeArquivo}`;
  };

  if (loading && !galeria) return <LoadingPage text={t('photoGalleries.loadingGallery')} />;
  if (error) return <ErrorPage message={error} onRetry={() => load(false)} />;
  if (!galeria) return <div>{t('photoGalleries.galleryNotFound')}</div>;

  // Calcular valores após garantir que galeria existe
  const imagemDestaqueUrl = getImagemUrl(galeria.imagemDestaque);
  const nomeArquivoDestaque = galeria.imagemDestaque ? galeria.imagemDestaque.split('/').pop() : null;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Button variant="ghost" asChild>
            <Link to="/galerias-fotos">
              <ArrowLeft className="h-4 w-4 mr-2" /> {t('photoGalleries.back')}
            </Link>
          </Button>
          <div>
            <h1 className="text-3xl font-bold">{galeria?.nome || t('photoGalleries.title')}</h1>
            <p className="text-muted-foreground">{galeria?.descricao || t('photoGalleries.uploadTitle')}</p>
          </div>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('photoGalleries.galleryInfo')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div>
              <label className="text-sm font-medium text-muted-foreground">{t('photoGalleries.date')}</label>
              <p className="text-base">{galeria?.data ? formatDate(galeria.data) : '-'}</p>
            </div>
            <div>
              <label className="text-sm font-medium text-muted-foreground">{t('photoGalleries.quantityPhotos')}</label>
              <p className="text-base">{galeria?.quantidadeFotos || 0}</p>
            </div>
            <div>
              <label className="text-sm font-medium text-muted-foreground">{t('photoGalleries.status')}</label>
              <p className="text-base">
                <span className={`px-2 py-1 rounded text-xs font-medium ${galeria?.ativo ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                  {galeria?.ativo ? t('photoGalleries.active') : t('photoGalleries.inactive')}
                </span>
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('photoGalleries.uploadTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div
            onDragEnter={handleDrag}
            onDragLeave={handleDrag}
            onDragOver={handleDrag}
            onDrop={handleDrop}
            className={`border-2 border-dashed rounded-lg p-8 text-center transition-colors ${
              dragActive ? 'border-primary bg-primary/5' : 'border-gray-300'
            }`}
          >
            <ImageIcon className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
            <p className="text-lg font-medium mb-2">{t('photoGalleries.dragHint')}</p>
            <p className="text-sm text-muted-foreground mb-4">
              {t('photoGalleries.formatsHint')}
            </p>
            <input
              type="file"
              id="file-upload"
              multiple
              accept="image/jpeg,image/jpg,image/png,image/gif,image/webp"
              onChange={(e) => handleFileSelect(e.target.files)}
              className="hidden"
            />
            <Button
              type="button"
              variant="outline"
              onClick={() => document.getElementById('file-upload').click()}
            >
              <Upload className="h-4 w-4 mr-2" /> {t('photoGalleries.selectFiles')}
            </Button>
          </div>

          {previewFiles.length > 0 && (
            <div className="mt-6">
              <div className="flex items-center justify-between mb-4">
                <p className="font-medium">{t('photoGalleries.selectedCount', { count: previewFiles.length })}</p>
                <Button onClick={handleUpload} disabled={uploading}>
                  {uploading ? t('photoGalleries.sending') : t('photoGalleries.sendPhotos', { count: previewFiles.length })}
                </Button>
              </div>
              <div className="grid grid-cols-4 gap-4">
                {previewFiles.map((preview, index) => (
                  <div key={index} className="relative group">
                    <img
                      src={preview.preview}
                      alt={`Preview ${index + 1}`}
                      className="w-full h-32 object-cover rounded"
                    />
                    <Button
                      variant="destructive"
                      size="sm"
                      className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity"
                      onClick={() => removeFile(index)}
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                ))}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {galeria?.quantidadeFotos > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>{t('photoGalleries.photosOfGallery', { count: galeria.quantidadeFotos })}</CardTitle>
          </CardHeader>
          <CardContent>
            {fotos.length > 0 ? (
              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
                {fotos.map((foto) => {
                  const isDestaque = foto.destaque || (galeria.imagemDestaque && foto.nomeArquivo === nomeArquivoDestaque);
                  const thumbnailUrl = getThumbnailUrl(foto.nomeArquivo);
                  
                  return (
                    <div
                      key={foto.nomeArquivo}
                      className="relative group cursor-pointer"
                      onClick={() => setViewingPhoto(foto.nomeArquivo)}
                    >
                      <div className="relative aspect-square rounded-lg overflow-hidden border-2 transition-all hover:border-primary bg-muted/30"
                        style={{ borderColor: isDestaque ? 'var(--primary)' : 'transparent' }}>
                        {thumbnailUrl && !imageErrors.has(foto.nomeArquivo) ? (
                          <img
                            src={thumbnailUrl}
                            alt={foto.nomeArquivo}
                            className="w-full h-full object-cover"
                            loading="lazy"
                            decoding="async"
                            onError={() => {
                              setImageErrors((prev) => new Set(prev).add(foto.nomeArquivo));
                            }}
                          />
                        ) : thumbnailUrl && imageErrors.has(foto.nomeArquivo) ? (
                          <div className="absolute inset-0 flex items-center justify-center bg-muted">
                            <ImageIcon className="h-8 w-8 text-muted-foreground" />
                          </div>
                        ) : (
                          <div className="w-full h-full bg-gray-100 flex items-center justify-center">
                            <ImageIcon className="h-8 w-8 text-gray-400" />
                          </div>
                        )}
                        
                        {isDestaque && (
                          <div className="absolute top-2 left-2 bg-primary text-white px-2 py-1 rounded text-xs font-medium flex items-center gap-1">
                            <Star className="h-3 w-3 fill-current" /> {t('photoGalleries.featured')}
                          </div>
                        )}
                        
                        <div className="absolute inset-0 bg-black/0 group-hover:bg-black/20 transition-colors flex items-center justify-center opacity-0 group-hover:opacity-100">
                          <div className="flex gap-2">
                            <Button
                              size="sm"
                              variant="secondary"
                              onClick={(e) => {
                                e.stopPropagation();
                                setViewingPhoto(foto.nomeArquivo);
                              }}
                            >
                              <Eye className="h-4 w-4" />
                            </Button>
                            {!isDestaque && (
                              <Button
                                size="sm"
                                variant="default"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  handleDefinirDestaque(foto.nomeArquivo);
                                }}
                              >
                                <Star className="h-4 w-4" />
                              </Button>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            ) : (
              <div className="text-center py-8">
                <ImageIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-600 mb-2">
                  {t('photoGalleries.noPhotosUpload')}
                </p>
                {imagemDestaqueUrl && (
                  <div className="mt-6">
                    <p className="text-sm font-medium mb-2">{t('photoGalleries.featuredImage')}</p>
                    <div className="relative inline-block">
                      <div className="relative w-48 h-48">
                        <img
                          src={imagemDestaqueUrl}
                          alt="Destaque"
                          className="w-full h-full object-cover rounded-lg border-4 border-primary cursor-pointer"
                          onClick={() => setViewingPhoto(nomeArquivoDestaque)}
                          onError={(e) => {
                            e.target.style.display = 'none';
                            const errorDiv = e.target.parentElement.querySelector('.error-fallback');
                            if (errorDiv) errorDiv.style.display = 'flex';
                          }}
                        />
                        <div className="error-fallback hidden absolute inset-0 w-full h-full bg-gray-100 rounded-lg border-4 border-red-300 items-center justify-center z-10">
                          <div className="text-center px-4 max-w-xs">
                            <ImageIcon className="h-12 w-12 text-red-400 mx-auto mb-2" />
                            <p className="text-xs text-red-600 font-medium mb-2">⚠️ {t('photoGalleries.imageNotFound')}</p>
                          </div>
                        </div>
                        <div className="absolute top-2 right-2 bg-primary text-white px-2 py-1 rounded text-xs font-medium flex items-center gap-1">
                          <Star className="h-3 w-3" /> {t('photoGalleries.featured')}
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {viewingPhoto && (
        <Dialog open={!!viewingPhoto} onOpenChange={() => setViewingPhoto(null)}>
          <DialogContent className="max-w-4xl">
            <DialogHeader>
              <DialogTitle>{t('photoGalleries.viewPhoto')}</DialogTitle>
            </DialogHeader>
            <div className="relative">
              <img
                src={getOriginalUrl(viewingPhoto)}
                alt={t('photoGalleries.viewPhoto')}
                className="w-full h-auto rounded"
              />
              {viewingPhoto !== nomeArquivoDestaque && (
                <Button
                  className="mt-4"
                  onClick={() => {
                    handleDefinirDestaque(viewingPhoto);
                    setViewingPhoto(null);
                  }}
                >
                  <Star className="h-4 w-4 mr-2" /> {t('photoGalleries.setAsFeatured')}
                </Button>
              )}
            </div>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}
