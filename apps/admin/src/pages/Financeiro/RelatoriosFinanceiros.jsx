import { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { FileText, Download, Calendar } from 'lucide-react';
import { relatoriosFinanceirosApi } from '@/lib/api';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { formatCurrency, formatDate } from '@/lib/formatters';

export default function RelatoriosFinanceiros() {
  const { t } = useTranslation();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [tipoRelatorio, setTipoRelatorio] = useState('fluxo-caixa');
  const [dataInicio, setDataInicio] = useState(() => {
    const date = new Date();
    date.setMonth(date.getMonth() - 1);
    return date.toISOString().split('T')[0];
  });
  const [dataFim, setDataFim] = useState(() => {
    return new Date().toISOString().split('T')[0];
  });
  const [relatorio, setRelatorio] = useState(null);

  const gerarRelatorio = async () => {
    if (!dataInicio || !dataFim) {
      toast.error(t('finance.reports.errorSelectDates'));
      return;
    }

    try {
      setLoading(true);
      setError(null);
      let response;

      switch (tipoRelatorio) {
        case 'fluxo-caixa':
          response = await relatoriosFinanceirosApi.getFluxoCaixa(dataInicio, dataFim);
          break;
        case 'por-categoria':
          response = await relatoriosFinanceirosApi.getPorCategoria(dataInicio, dataFim);
          break;
        case 'por-centro-custo':
          response = await relatoriosFinanceirosApi.getPorCentroCusto(dataInicio, dataFim);
          break;
        case 'por-projeto':
          response = await relatoriosFinanceirosApi.getPorProjeto(dataInicio, dataFim);
          break;
        default:
          throw new Error('Tipo de relatório inválido');
      }

      setRelatorio(response.data);
      toast.success(t('finance.reports.success'));
    } catch (err) {
      setError(t('finance.reports.errorLoad'));
      console.error(err);
      toast.error(t('finance.reports.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  const renderRelatorio = () => {
    if (!relatorio) return null;

    switch (tipoRelatorio) {
      case 'fluxo-caixa':
        return (
          <Card>
            <CardHeader>
              <CardTitle>{t('finance.reports.cashFlow.title')}</CardTitle>
              <p className="text-sm text-muted-foreground">
                {t('finance.reports.periodLabel', { start: formatDate(relatorio.dataInicio), end: formatDate(relatorio.dataFim) })}
              </p>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="grid grid-cols-3 gap-4">
                  <div>
                    <p className="text-sm text-muted-foreground">{t('finance.reports.cashFlow.summary.totalRevenues')}</p>
                    <p className="text-2xl font-bold text-green-600">{formatCurrency(relatorio.totalReceitas)}</p>
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">{t('finance.reports.cashFlow.summary.totalExpenses')}</p>
                    <p className="text-2xl font-bold text-red-600">{formatCurrency(relatorio.totalDespesas)}</p>
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">{t('finance.reports.cashFlow.summary.balance')}</p>
                    <p className={`text-2xl font-bold ${relatorio.saldo >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                      {formatCurrency(relatorio.saldo)}
                    </p>
                  </div>
                </div>
                {relatorio.movimentacoesDiarias && relatorio.movimentacoesDiarias.length > 0 && (
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>{t('finance.reports.cashFlow.table.date')}</TableHead>
                        <TableHead className="text-right">{t('finance.reports.cashFlow.table.revenues')}</TableHead>
                        <TableHead className="text-right">{t('finance.reports.cashFlow.table.expenses')}</TableHead>
                        <TableHead className="text-right">{t('finance.reports.cashFlow.table.dayBalance')}</TableHead>
                        <TableHead className="text-right">{t('finance.reports.cashFlow.table.accumulatedBalance')}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {relatorio.movimentacoesDiarias.map((item, idx) => (
                        <TableRow key={idx}>
                          <TableCell>{formatDate(item.data)}</TableCell>
                          <TableCell className="text-right text-green-600">{formatCurrency(item.receitas)}</TableCell>
                          <TableCell className="text-right text-red-600">{formatCurrency(item.despesas)}</TableCell>
                          <TableCell className={`text-right font-bold ${item.saldoDia >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                            {formatCurrency(item.saldoDia)}
                          </TableCell>
                          <TableCell className={`text-right font-bold ${item.saldoAcumulado >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                            {formatCurrency(item.saldoAcumulado)}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </div>
            </CardContent>
          </Card>
        );

      case 'por-categoria':
        return (
          <Card>
            <CardHeader>
              <CardTitle>{t('finance.reports.byCategory.title')}</CardTitle>
              <p className="text-sm text-muted-foreground">
                {t('finance.reports.periodLabel', { start: formatDate(dataInicio), end: formatDate(dataFim) })}
              </p>
            </CardHeader>
            <CardContent>
              <div className="space-y-6">
                {relatorio.receitas && relatorio.receitas.length > 0 && (
                  <div>
                    <h3 className="text-lg font-semibold mb-3 text-green-600">{t('finance.reports.byCategory.revenuesTitle')}</h3>
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>{t('finance.reports.byCategory.table.category')}</TableHead>
                          <TableHead className="text-right">{t('finance.reports.byCategory.table.value')}</TableHead>
                          <TableHead className="text-right">{t('finance.reports.byCategory.table.quantity')}</TableHead>
                          <TableHead className="text-right">{t('finance.reports.byCategory.table.percent')}</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {relatorio.receitas.map((item, idx) => (
                          <TableRow key={idx}>
                            <TableCell className="font-medium">{item.categoriaNome || t('finance.common.notCategorized')}</TableCell>
                            <TableCell className="text-right text-green-600">{formatCurrency(item.valor)}</TableCell>
                            <TableCell className="text-right">{item.quantidade}</TableCell>
                            <TableCell className="text-right">{item.percentual.toFixed(1)}%</TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </div>
                )}
                {relatorio.despesas && relatorio.despesas.length > 0 && (
                  <div>
                    <h3 className="text-lg font-semibold mb-3 text-red-600">{t('finance.reports.byCategory.expensesTitle')}</h3>
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>{t('finance.reports.byCategory.table.category')}</TableHead>
                          <TableHead className="text-right">{t('finance.reports.byCategory.table.value')}</TableHead>
                          <TableHead className="text-right">{t('finance.reports.byCategory.table.quantity')}</TableHead>
                          <TableHead className="text-right">{t('finance.reports.byCategory.table.percent')}</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {relatorio.despesas.map((item, idx) => (
                          <TableRow key={idx}>
                            <TableCell className="font-medium">{item.categoriaNome || t('finance.common.notCategorized')}</TableCell>
                            <TableCell className="text-right text-red-600">{formatCurrency(item.valor)}</TableCell>
                            <TableCell className="text-right">{item.quantidade}</TableCell>
                            <TableCell className="text-right">{item.percentual.toFixed(1)}%</TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        );

      case 'por-centro-custo':
        return (
          <Card>
            <CardHeader>
              <CardTitle>{t('finance.reports.byCostCenter.title')}</CardTitle>
              <p className="text-sm text-muted-foreground">
                {t('finance.reports.periodLabel', { start: formatDate(dataInicio), end: formatDate(dataFim) })}
              </p>
            </CardHeader>
            <CardContent>
              {relatorio.length === 0 ? (
                <div className="text-center py-8 text-muted-foreground">{t('finance.reports.byCostCenter.empty')}</div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{t('finance.reports.byCostCenter.table.costCenter')}</TableHead>
                      <TableHead className="text-right">{t('finance.reports.byCostCenter.table.revenues')}</TableHead>
                      <TableHead className="text-right">{t('finance.reports.byCostCenter.table.expenses')}</TableHead>
                      <TableHead className="text-right">{t('finance.reports.byCostCenter.table.balance')}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {relatorio.map((item, idx) => (
                      <TableRow key={idx}>
                        <TableCell className="font-medium">{item.centroCusto || t('finance.common.noCostCenter')}</TableCell>
                        <TableCell className="text-right text-green-600">{formatCurrency(item.totalReceitas)}</TableCell>
                        <TableCell className="text-right text-red-600">{formatCurrency(item.totalDespesas)}</TableCell>
                        <TableCell className={`text-right font-bold ${item.saldo >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                          {formatCurrency(item.saldo)}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        );

      case 'por-projeto':
        return (
          <Card>
            <CardHeader>
              <CardTitle>{t('finance.reports.byProject.title')}</CardTitle>
              <p className="text-sm text-muted-foreground">
                {t('finance.reports.periodLabel', { start: formatDate(dataInicio), end: formatDate(dataFim) })}
              </p>
            </CardHeader>
            <CardContent>
              {relatorio.length === 0 ? (
                <div className="text-center py-8 text-muted-foreground">{t('finance.reports.byProject.empty')}</div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{t('finance.reports.byProject.table.project')}</TableHead>
                      <TableHead className="text-right">{t('finance.reports.byProject.table.budget')}</TableHead>
                      <TableHead className="text-right">{t('finance.reports.byProject.table.revenues')}</TableHead>
                      <TableHead className="text-right">{t('finance.reports.byProject.table.expenses')}</TableHead>
                      <TableHead className="text-right">{t('finance.reports.byProject.table.balance')}</TableHead>
                      <TableHead className="text-right">{t('finance.reports.byProject.table.usedPercent')}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {relatorio.map((item, idx) => (
                      <TableRow key={idx}>
                        <TableCell className="font-medium">{item.projeto || t('finance.common.noProject')}</TableCell>
                        <TableCell className="text-right">{item.orcamento ? formatCurrency(item.orcamento) : '-'}</TableCell>
                        <TableCell className="text-right text-green-600">{formatCurrency(item.totalReceitas)}</TableCell>
                        <TableCell className="text-right text-red-600">{formatCurrency(item.totalDespesas)}</TableCell>
                        <TableCell className={`text-right font-bold ${item.saldo >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                          {formatCurrency(item.saldo)}
                        </TableCell>
                        <TableCell className="text-right">
                          {item.percentualUtilizado !== null && item.percentualUtilizado !== undefined
                            ? `${item.percentualUtilizado.toFixed(1)}%`
                            : '-'}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        );

      default:
        return null;
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('finance.reports.title')}</h1>
        <p className="text-muted-foreground">{t('finance.reports.subtitle')}</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5" />
            {t('finance.reports.paramsTitle')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4">
            <div className="space-y-2">
              <Label htmlFor="tipoRelatorio">{t('finance.reports.typeLabel')}</Label>
              <Select value={tipoRelatorio} onValueChange={setTipoRelatorio}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="fluxo-caixa">{t('finance.reports.type.cashFlow')}</SelectItem>
                  <SelectItem value="por-categoria">{t('finance.reports.type.byCategory')}</SelectItem>
                  <SelectItem value="por-centro-custo">{t('finance.reports.type.byCostCenter')}</SelectItem>
                  <SelectItem value="por-projeto">{t('finance.reports.type.byProject')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="dataInicio">{t('finance.reports.startDate')}</Label>
              <Input
                id="dataInicio"
                type="date"
                value={dataInicio}
                onChange={(e) => setDataInicio(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="dataFim">{t('finance.reports.endDate')}</Label>
              <Input
                id="dataFim"
                type="date"
                value={dataFim}
                onChange={(e) => setDataFim(e.target.value)}
              />
            </div>
            <div className="space-y-2 flex items-end">
              <Button onClick={gerarRelatorio} disabled={loading} className="w-full">
                <Calendar className="h-4 w-4 mr-2" />
                {loading ? t('finance.reports.generating') : t('finance.reports.generateButton')}
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {loading && <LoadingPage text={t('finance.reports.generatingMessage')} />}
      {error && <ErrorPage message={error} onRetry={gerarRelatorio} />}
      {relatorio && !loading && renderRelatorio()}
    </div>
  );
}
