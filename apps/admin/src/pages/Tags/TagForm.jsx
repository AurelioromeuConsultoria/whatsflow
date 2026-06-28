import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { tagsApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';

export default function TagForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);

  const [formData, setFormData] = useState({ nome: '', cor: '#25D366' });
  const [loading, setLoading] = useState(false);
  const [pageLoading, setPageLoading] = useState(isEditing);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (!isEditing) return;
    const load = async () => {
      try {
        setPageLoading(true);
        setError(null);
        const res = await tagsApi.getById(id);
        setFormData({ nome: res.data.nome || '', cor: res.data.cor || '#25D366' });
      } catch (err) {
        setError(getApiErrorMessage(err, 'Erro ao carregar tag.'));
      } finally {
        setPageLoading(false);
      }
    };
    load();
  }, [id, isEditing]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.nome.trim()) {
      toast.error('O nome é obrigatório.');
      return;
    }
    try {
      setLoading(true);
      const payload = { nome: formData.nome.trim(), cor: formData.cor || null };
      if (isEditing) await tagsApi.update(id, payload);
      else await tagsApi.create(payload);
      toast.success(isEditing ? 'Tag atualizada.' : 'Tag criada.');
      navigate('/tags');
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao salvar tag.'));
    } finally {
      setLoading(false);
    }
  };

  if (pageLoading) return <LoadingPage text="Carregando tag..." />;
  if (error) return <ErrorPage message={error} onRetry={() => window.location.reload()} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/tags"><ArrowLeft className="h-4 w-4 mr-2" /> Voltar</Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? 'Editar tag' : 'Nova tag'}</h1>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Dados da tag</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4 max-w-md">
            <div className="space-y-2">
              <Label htmlFor="nome">Nome *</Label>
              <Input id="nome" value={formData.nome} onChange={(e) => setFormData((prev) => ({ ...prev, nome: e.target.value }))} required />
            </div>
            <div className="space-y-2">
              <Label htmlFor="cor">Cor</Label>
              <div className="flex items-center gap-3">
                <input
                  id="cor"
                  type="color"
                  value={formData.cor || '#25D366'}
                  onChange={(e) => setFormData((prev) => ({ ...prev, cor: e.target.value }))}
                  className="h-10 w-14 cursor-pointer rounded border border-input bg-background"
                />
                <Input
                  value={formData.cor}
                  onChange={(e) => setFormData((prev) => ({ ...prev, cor: e.target.value }))}
                  placeholder="#25D366"
                  className="max-w-[160px]"
                />
              </div>
            </div>
            <div className="flex items-center space-x-4 pt-2">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? 'Salvando...' : (isEditing ? 'Atualizar' : 'Criar')}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/tags">Cancelar</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
