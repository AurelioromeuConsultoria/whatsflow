import { lazy, Suspense, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, Download, FileText } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { patrimonioApi, categoriasPatrimonioApi } from '@/lib/api';
import { formatCurrency, formatDate } from '@/lib/formatters';
import { useTranslation } from 'react-i18next';

const PatrimonioRelatorioCharts = lazy(() => import('./PatrimonioRelatorioCharts'));

const statusKeyMap = {
  EmUso: 'inUse',
  EmManutencao: 'inMaintenance',
  Emprestado: 'loaned',
  Ocioso: 'idle',
  Baixado: 'disposed',
};

const normalizeText = (value, fallback) => {
  const text = String(value || '').trim();
  return text || fallback;
};

export default function PatrimonioRelatorio() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [categorias, setCategorias] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [categoriaId, setCategoriaId] = useState('');
  const [status, setStatus] = useState('');
  const [campus, setCampus] = useState('');
  const [localizacao, setLocalizacao] = useState('');

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const [itemsRes, categoriasRes] = await Promise.all([
        patrimonioApi.getAll(),
        categoriasPatrimonioApi.getAll(),
      ]);

      setItems(itemsRes.data || []);
      setCategorias(categoriasRes.data || []);
    } catch (err) {
      setError(t('finance.patrimonyReport.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const filteredItems = useMemo(() => items.filter((item) => {
    if (categoriaId && String(item.categoriaPatrimonioId) !== categoriaId) return false;
    if (status && item.status !== status) return false;

    const campusBusca = campus.trim().toLowerCase();
    if (campusBusca && !String(item.campus || '').toLowerCase().includes(campusBusca)) return false;

    const localizacaoBusca = localizacao.trim().toLowerCase();
    if (localizacaoBusca && !String(item.localizacao || '').toLowerCase().includes(localizacaoBusca)) return false;

    return true;
  }), [items, categoriaId, status, campus, localizacao]);

  const resumo = useMemo(() => ({
    totalItens: filteredItems.length,
    totalAtivos: filteredItems.filter((item) => item.ativo).length,
    valorTotal: filteredItems.reduce((acc, item) => acc + Number(item.valorAquisicao || 0), 0),
    totalLocais: new Set(filteredItems.map((item) => normalizeText(item.localizacao, t('finance.patrimonyReport.notInformed')))).size,
  }), [filteredItems, t]);

  const consolidadoPorCategoria = useMemo(() => {
    const grouped = filteredItems.reduce((acc, item) => {
      const nome = normalizeText(item.categoriaNome, t('finance.patrimonyReport.notInformed'));
      const current = acc.get(nome) || { nome, quantidade: 0, valorTotal: 0 };
      current.quantidade += Number(item.quantidade || 0);
      current.valorTotal += Number(item.valorAquisicao || 0);
      acc.set(nome, current);
      return acc;
    }, new Map());

    return Array.from(grouped.values()).sort((a, b) => b.valorTotal - a.valorTotal || b.quantidade - a.quantidade);
  }, [filteredItems, t]);

  const consolidadoPorLocal = useMemo(() => {
    const grouped = filteredItems.reduce((acc, item) => {
      const campusNome = normalizeText(item.campus, t('finance.patrimonyReport.notInformed'));
      const localNome = normalizeText(item.localizacao, t('finance.patrimonyReport.notInformed'));
      const chave = `${campusNome}__${localNome}`;
      const current = acc.get(chave) || {
        campus: campusNome,
        localizacao: localNome,
        quantidade: 0,
        valorTotal: 0,
      };

      current.quantidade += Number(item.quantidade || 0);
      current.valorTotal += Number(item.valorAquisicao || 0);
      acc.set(chave, current);
      return acc;
    }, new Map());

    return Array.from(grouped.values()).sort((a, b) => b.valorTotal - a.valorTotal || b.quantidade - a.quantidade);
  }, [filteredItems, t]);

  const consolidadoPorStatus = useMemo(() => {
    const grouped = filteredItems.reduce((acc, item) => {
      const nome = item.status || 'EmUso';
      const current = acc.get(nome) || { status: nome, quantidade: 0, valorTotal: 0 };
      current.quantidade += Number(item.quantidade || 0);
      current.valorTotal += Number(item.valorAquisicao || 0);
      acc.set(nome, current);
      return acc;
    }, new Map());

    return Array.from(grouped.values()).sort((a, b) => b.quantidade - a.quantidade || b.valorTotal - a.valorTotal);
  }, [filteredItems]);

  const alertas = useMemo(() => {
    const hoje = new Date();
    const limite = new Date();
    limite.setDate(hoje.getDate() + 30);

    return {
      garantiaProxima: filteredItems
        .filter((item) => item.garantiaAte && new Date(item.garantiaAte) >= hoje && new Date(item.garantiaAte) <= limite)
        .sort((a, b) => new Date(a.garantiaAte) - new Date(b.garantiaAte))
        .slice(0, 5),
      manutencaoPendente: filteredItems
        .filter((item) => item.dataProximaManutencao && new Date(item.dataProximaManutencao) <= limite)
        .sort((a, b) => new Date(a.dataProximaManutencao) - new Date(b.dataProximaManutencao))
        .slice(0, 5),
    };
  }, [filteredItems]);

  const exportarCsv = () => {
    const linhas = [
      [
        t('finance.patrimony.table.code'),
        t('finance.patrimony.table.name'),
        t('finance.patrimony.table.category'),
        t('finance.patrimony.fields.campus'),
        t('finance.patrimony.table.location'),
        t('finance.patrimony.table.status'),
        t('finance.patrimony.fields.responsible'),
        t('finance.patrimony.table.value'),
      ],
      ...filteredItems.map((item) => ([
        item.codigo || '',
        item.nome || '',
        item.categoriaNome || t('finance.patrimonyReport.notInformed'),
        item.campus || t('finance.patrimonyReport.notInformed'),
        item.localizacao || t('finance.patrimonyReport.notInformed'),
        t(`finance.patrimony.status.${statusKeyMap[item.status] || 'inUse'}`),
        item.responsavelNome || t('finance.patrimonyReport.notInformed'),
        Number(item.valorAquisicao || 0).toFixed(2),
      ])),
    ];

    const csvContent = linhas
      .map((linha) => linha.map((valor) => `"${String(valor).replaceAll('"', '""')}"`).join(';'))
      .join('\n');

    const blob = new Blob([`\uFEFF${csvContent}`], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `relatorio-patrimonio-${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  if (loading) return <LoadingPage text={t('finance.patrimonyReport.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('finance.patrimonyReport.title')}</h1>
          <p className="text-muted-foreground">{t('finance.patrimonyReport.subtitle')}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" onClick={exportarCsv} disabled={filteredItems.length === 0}>
            <Download className="mr-2 h-4 w-4" />
            {t('finance.patrimonyReport.export')}
          </Button>
          <Button variant="outline" asChild>
            <Link to="/financeiro/patrimonio">
              <ArrowLeft className="mr-2 h-4 w-4" />
              {t('finance.patrimonyReport.backToList')}
            </Link>
          </Button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">{t('finance.patrimonyReport.summary.totalItems')}</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.totalItens}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">{t('finance.patrimonyReport.summary.activeItems')}</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.totalAtivos}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">{t('finance.patrimonyReport.summary.totalValue')}</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-bold">{formatCurrency(resumo.valorTotal)}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">{t('finance.patrimonyReport.summary.totalLocations')}</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-bold">{resumo.totalLocais}</CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('finance.common.filters')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('finance.patrimony.fields.category')}</label>
              <select value={categoriaId} onChange={(e) => setCategoriaId(e.target.value)} className="w-full rounded border px-3 py-2">
                <option value="">{t('finance.patrimony.filters.allCategories')}</option>
                {categorias.map((categoria) => (
                  <option key={categoria.id} value={categoria.id}>{categoria.nome}</option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('finance.patrimony.fields.status')}</label>
              <select value={status} onChange={(e) => setStatus(e.target.value)} className="w-full rounded border px-3 py-2">
                <option value="">{t('finance.patrimony.filters.allStatus')}</option>
                {Object.entries(statusKeyMap).map(([value, key]) => (
                  <option key={value} value={value}>{t(`finance.patrimony.status.${key}`)}</option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('finance.patrimony.fields.campus')}</label>
              <Input value={campus} onChange={(e) => setCampus(e.target.value)} placeholder={t('finance.patrimonyReport.filters.campusPlaceholder')} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('finance.patrimony.fields.location')}</label>
              <Input value={localizacao} onChange={(e) => setLocalizacao(e.target.value)} placeholder={t('finance.patrimonyReport.filters.locationPlaceholder')} />
            </div>
          </div>
        </CardContent>
      </Card>

      <Suspense
        fallback={
          <div className="grid gap-6 xl:grid-cols-3">
            {[0, 1, 2].map((item) => (
              <Card key={item}>
                <CardHeader>
                  <CardTitle>{t('finance.patrimonyReport.loadingCharts')}</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="h-[260px] animate-pulse rounded-lg bg-muted" />
                </CardContent>
              </Card>
            ))}
          </div>
        }
      >
        <PatrimonioRelatorioCharts
          t={t}
          consolidadoPorCategoria={consolidadoPorCategoria}
          consolidadoPorStatus={consolidadoPorStatus}
          consolidadoPorLocal={consolidadoPorLocal}
          statusKeyMap={statusKeyMap}
          formatCurrency={formatCurrency}
        />
      </Suspense>

      <div className="grid gap-6 xl:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle>{t('finance.patrimonyReport.sections.byCategory')}</CardTitle>
          </CardHeader>
          <CardContent>
            {consolidadoPorCategoria.length === 0 ? (
              <div className="py-8 text-center text-muted-foreground">{t('finance.patrimonyReport.empty')}</div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('finance.patrimonyReport.table.category')}</TableHead>
                    <TableHead className="text-right">{t('finance.patrimonyReport.table.quantity')}</TableHead>
                    <TableHead className="text-right">{t('finance.patrimonyReport.table.value')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {consolidadoPorCategoria.map((item) => (
                    <TableRow key={item.nome}>
                      <TableCell className="font-medium">{item.nome}</TableCell>
                      <TableCell className="text-right">{item.quantidade}</TableCell>
                      <TableCell className="text-right">{formatCurrency(item.valorTotal)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('finance.patrimonyReport.sections.byLocation')}</CardTitle>
          </CardHeader>
          <CardContent>
            {consolidadoPorLocal.length === 0 ? (
              <div className="py-8 text-center text-muted-foreground">{t('finance.patrimonyReport.empty')}</div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('finance.patrimonyReport.table.campus')}</TableHead>
                    <TableHead>{t('finance.patrimonyReport.table.location')}</TableHead>
                    <TableHead className="text-right">{t('finance.patrimonyReport.table.quantity')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {consolidadoPorLocal.map((item) => (
                    <TableRow key={`${item.campus}-${item.localizacao}`}>
                      <TableCell className="font-medium">{item.campus}</TableCell>
                      <TableCell>{item.localizacao}</TableCell>
                      <TableCell className="text-right">{item.quantidade}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('finance.patrimonyReport.sections.byStatus')}</CardTitle>
          </CardHeader>
          <CardContent>
            {consolidadoPorStatus.length === 0 ? (
              <div className="py-8 text-center text-muted-foreground">{t('finance.patrimonyReport.empty')}</div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('finance.patrimonyReport.table.status')}</TableHead>
                    <TableHead className="text-right">{t('finance.patrimonyReport.table.quantity')}</TableHead>
                    <TableHead className="text-right">{t('finance.patrimonyReport.table.value')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {consolidadoPorStatus.map((item) => (
                    <TableRow key={item.status}>
                      <TableCell className="font-medium">{t(`finance.patrimony.status.${statusKeyMap[item.status] || 'inUse'}`)}</TableCell>
                      <TableCell className="text-right">{item.quantidade}</TableCell>
                      <TableCell className="text-right">{formatCurrency(item.valorTotal)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('finance.patrimonyReport.sections.warrantyAlerts')}</CardTitle>
          </CardHeader>
          <CardContent>
            {alertas.garantiaProxima.length === 0 ? (
              <div className="py-8 text-center text-muted-foreground">{t('finance.patrimonyReport.noAlerts')}</div>
            ) : (
              <div className="space-y-3">
                {alertas.garantiaProxima.map((item) => (
                  <div key={item.id} className="rounded-lg border p-3">
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <p className="font-medium">{item.nome}</p>
                        <p className="text-sm text-muted-foreground">{item.codigo} • {item.localizacao || t('finance.patrimonyReport.notInformed')}</p>
                      </div>
                      <p className="text-sm font-medium">{formatDate(item.garantiaAte)}</p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('finance.patrimonyReport.sections.maintenanceAlerts')}</CardTitle>
          </CardHeader>
          <CardContent>
            {alertas.manutencaoPendente.length === 0 ? (
              <div className="py-8 text-center text-muted-foreground">{t('finance.patrimonyReport.noAlerts')}</div>
            ) : (
              <div className="space-y-3">
                {alertas.manutencaoPendente.map((item) => (
                  <div key={item.id} className="rounded-lg border p-3">
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <p className="font-medium">{item.nome}</p>
                        <p className="text-sm text-muted-foreground">{item.codigo} • {item.localizacao || t('finance.patrimonyReport.notInformed')}</p>
                      </div>
                      <p className="text-sm font-medium">{formatDate(item.dataProximaManutencao)}</p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-4 w-4" />
            {t('finance.patrimonyReport.sections.filteredItems')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {filteredItems.length === 0 ? (
            <div className="py-8 text-center text-muted-foreground">{t('finance.patrimonyReport.empty')}</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('finance.patrimony.table.code')}</TableHead>
                  <TableHead>{t('finance.patrimony.table.name')}</TableHead>
                  <TableHead>{t('finance.patrimony.table.category')}</TableHead>
                  <TableHead>{t('finance.patrimony.table.location')}</TableHead>
                  <TableHead>{t('finance.patrimony.table.status')}</TableHead>
                  <TableHead className="text-right">{t('finance.patrimony.table.value')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredItems.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell className="font-medium">{item.codigo}</TableCell>
                    <TableCell>
                      <Link to={`/financeiro/patrimonio/${item.id}`} className="hover:underline">
                        {item.nome}
                      </Link>
                    </TableCell>
                    <TableCell>{item.categoriaNome || t('finance.patrimonyReport.notInformed')}</TableCell>
                    <TableCell>{item.localizacao || t('finance.patrimonyReport.notInformed')}</TableCell>
                    <TableCell>{t(`finance.patrimony.status.${statusKeyMap[item.status] || 'inUse'}`)}</TableCell>
                    <TableCell className="text-right">{formatCurrency(item.valorAquisicao)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
