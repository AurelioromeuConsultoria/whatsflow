import { useState, useRef } from 'react';
import { Upload, X, Image as ImageIcon, Video, Music, FileText, File, Eye, ExternalLink, Loader2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { getAbsoluteUrl } from '@/lib/utils';
import { toast } from 'sonner';
import api from '@/lib/api';

const MAX_FILE_SIZE = 500 * 1024 * 1024; // 500MB

const FILE_TYPE_ICONS = {
  image: ImageIcon,
  video: Video,
  audio: Music,
  pdf: FileText,
  file: File,
};

function detectFileType(url, accept, type) {
  if (type && type !== 'auto') return type;
  
  if (accept) {
    if (accept.includes('image')) return 'image';
    if (accept.includes('video')) return 'video';
    if (accept.includes('audio')) return 'audio';
    if (accept.includes('.pdf') || accept.includes('pdf')) return 'pdf';
  }
  
  if (url) {
    const lowerUrl = url.toLowerCase();
    if (lowerUrl.match(/\.(jpg|jpeg|png|gif|webp|svg)$/)) return 'image';
    if (lowerUrl.match(/\.(mp4|webm|ogg|mov)$/)) return 'video';
    if (lowerUrl.match(/\.(mp3|wav|ogg|m4a)$/)) return 'audio';
    if (lowerUrl.match(/\.pdf$/)) return 'pdf';
  }
  
  return 'file';
}

function getFileTypeIcon(fileType) {
  return FILE_TYPE_ICONS[fileType] || File;
}

function getFileTypeLabel(fileType, t) {
  const labels = {
    image: t('imageUpload.fileTypes.image'),
    video: t('imageUpload.fileTypes.video'),
    audio: t('imageUpload.fileTypes.audio'),
    pdf: t('imageUpload.fileTypes.pdf'),
    file: t('imageUpload.fileTypes.file'),
  };

  return labels[fileType] || labels.file;
}

function getUploadEndpoint(fileType) {
  const endpoints = {
    image: '/admin/upload/images',
    video: '/admin/upload/videos',
    audio: '/admin/upload/audios',
    file: '/admin/upload/files',
  };
  return endpoints[fileType] || endpoints.file;
}

export function ImageUpload({
  value,
  onChange,
  accept,
  label,
  required = false,
  type = 'auto',
}) {
  const { t } = useTranslation();
  const [uploading, setUploading] = useState(false);
  const [previewOpen, setPreviewOpen] = useState(false);
  const fileInputRef = useRef(null);
  
  const fileType = detectFileType(value, accept, type);
  const FileIcon = getFileTypeIcon(fileType);
  const fileTypeLabel = getFileTypeLabel(fileType, t);
  const absoluteUrl = value ? getAbsoluteUrl(value) : null;
  
  const handleFileSelect = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    
    // Validar tamanho
    if (file.size > MAX_FILE_SIZE) {
      toast.error(t('imageUpload.fileTooLarge', { size: '500MB' }));
      return;
    }
    
    // Validar tipo se accept estiver definido
    if (accept && !accept.split(',').some(accepted => {
      const trimmed = accepted.trim();
      if (trimmed.startsWith('.')) {
        return file.name.toLowerCase().endsWith(trimmed.toLowerCase());
      }
      return file.type.match(new RegExp(trimmed.replace('*', '.*')));
    })) {
      toast.error(t('imageUpload.fileTypeNotAllowed', { accept }));
      return;
    }
    
    try {
      setUploading(true);
      const formData = new FormData();
      formData.append('file', file);
      
      const endpoint = getUploadEndpoint(fileType);
      const response = await api.post(endpoint, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });
      
      // A API pode retornar url, path ou o próprio caminho como string
      const uploadedUrl = response.data?.url || response.data?.path || response.data || '';
      onChange(uploadedUrl);
      toast.success(t('imageUpload.uploadSuccess', { fileType: fileTypeLabel }));
    } catch (error) {
      const errorMessage = error.response?.data?.message || t('imageUpload.uploadError', { fileType: fileTypeLabel.toLowerCase() });
      toast.error(errorMessage);
      console.error(error);
    } finally {
      setUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };
  
  const handleRemove = () => {
    onChange('');
  };
  
  const handleOpenUrl = () => {
    if (absoluteUrl) {
      window.open(absoluteUrl, '_blank');
    }
  };
  
  const renderPreview = () => {
    if (!absoluteUrl) return null;
    
    const size = 80;
    
    switch (fileType) {
      case 'image':
        return (
          <img
            src={absoluteUrl}
            alt={t('imageUpload.previewAlt')}
            className="rounded object-cover border"
            style={{ width: size, height: size }}
            onError={(e) => {
              e.target.style.display = 'none';
            }}
          />
        );
      case 'video':
        return (
          <div className="flex items-center justify-center rounded border bg-muted" style={{ width: size, height: size }}>
            <Video className="h-8 w-8 text-muted-foreground" />
          </div>
        );
      case 'audio':
        return (
          <div className="flex items-center justify-center rounded border bg-muted" style={{ width: size, height: size }}>
            <Music className="h-8 w-8 text-muted-foreground" />
          </div>
        );
      case 'pdf':
        return (
          <div className="flex items-center justify-center rounded border bg-muted" style={{ width: size, height: size }}>
            <FileText className="h-8 w-8 text-muted-foreground" />
          </div>
        );
      default:
        return (
          <div className="flex items-center justify-center rounded border bg-muted" style={{ width: size, height: size }}>
            <File className="h-8 w-8 text-muted-foreground" />
          </div>
        );
    }
  };
  
  const renderModalContent = () => {
    if (!absoluteUrl) return null;
    
    switch (fileType) {
      case 'image':
        return (
          <img
            src={absoluteUrl}
            alt={t('imageUpload.previewTitle', { fileType: fileTypeLabel })}
            className="max-h-[80vh] w-auto mx-auto rounded"
          />
        );
      case 'video':
        return (
          <video
            src={absoluteUrl}
            controls
            className="max-h-[80vh] w-full rounded"
          >
            {t('imageUpload.browserNotSupportVideo')}
          </video>
        );
      case 'audio':
        return (
          <div className="p-8">
            <audio src={absoluteUrl} controls className="w-full">
              {t('imageUpload.browserNotSupportAudio')}
            </audio>
          </div>
        );
      case 'pdf':
        return (
          <iframe
            src={absoluteUrl}
            className="w-full h-[80vh] rounded border"
            title={t('imageUpload.pdfTitle')}
          />
        );
      default:
        return (
          <div className="p-8 text-center">
            <File className="h-16 w-16 mx-auto mb-4 text-muted-foreground" />
            <p className="text-muted-foreground">{t('imageUpload.previewUnavailable')}</p>
            <Button onClick={handleOpenUrl} className="mt-4" variant="outline">
              <ExternalLink className="h-4 w-4 mr-2" />
              {t('imageUpload.openUrl')}
            </Button>
          </div>
        );
    }
  };
  
  return (
    <div className="space-y-2">
      {label && (
        <Label htmlFor={`image-upload-${label}`}>
          {label} {required && <span className="text-destructive">*</span>}
        </Label>
      )}
      
      <div className="flex items-start gap-4">
        {absoluteUrl && (
          <div className="flex-shrink-0">
            {renderPreview()}
          </div>
        )}
        
        <div className="flex-1 space-y-2">
          <div className="flex items-center gap-2 flex-wrap">
            <input
              ref={fileInputRef}
              type="file"
              id={`image-upload-${label}`}
              accept={accept}
              onChange={handleFileSelect}
              className="hidden"
              disabled={uploading}
            />
            
            <Button
              type="button"
              variant="outline"
              onClick={() => fileInputRef.current?.click()}
              disabled={uploading}
            >
              {uploading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  {t('imageUpload.uploading')}
                </>
              ) : (
                <>
                  <Upload className="h-4 w-4 mr-2" />
                  {absoluteUrl ? t('imageUpload.replace') : t('imageUpload.upload', { fileType: fileTypeLabel })}
                </>
              )}
            </Button>
            
            {absoluteUrl && (
              <>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setPreviewOpen(true)}
                >
                  <Eye className="h-4 w-4 mr-2" />
                  {t('imageUpload.view', { fileType: fileTypeLabel })}
                </Button>
                
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleOpenUrl}
                >
                  <ExternalLink className="h-4 w-4 mr-2" />
                  {t('imageUpload.openUrl')}
                </Button>
                
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleRemove}
                  disabled={uploading}
                >
                  <X className="h-4 w-4 mr-2" />
                  {t('imageUpload.remove')}
                </Button>
              </>
            )}
          </div>
          
          {absoluteUrl && (
            <p className="text-xs text-muted-foreground break-all">
              {value}
            </p>
          )}
        </div>
      </div>
      
      <Dialog open={previewOpen} onOpenChange={setPreviewOpen}>
        <DialogContent className="max-w-4xl">
          <DialogHeader>
            <DialogTitle>{t('imageUpload.previewDialogTitle', { fileType: fileTypeLabel })}</DialogTitle>
          </DialogHeader>
          <div className="flex justify-center items-center min-h-[200px]">
            {renderModalContent()}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
