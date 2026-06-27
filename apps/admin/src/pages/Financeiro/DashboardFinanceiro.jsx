import { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { TrendingUp, TrendingDown, DollarSign, Calendar, PieChart, BarChart3 } from 'lucide-react';
import { dashboardFinanceiroApi } from '@/lib/api';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { Skeleton } from '@/components/ui/skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useTranslation } from 'react-i18next';
import { formatCurrency, formatDate } from '@/lib/formatters';

export default function DashboardFinanceiro() {
  const { t } = useTranslation();
  const [dashboard, setDashboard] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await dashboardFinanceiroApi.getDashboard();
      setDashboard(response.data);
    } catch (err) {
      console.error('Erro ao carregar dashboard financeiro:', err);
      setError(t('finance.dashboard.errorLoad', 'Erro ao carregar dashboard financeiro'));
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <Skeleton className="h-9 w-48 mb-2" />
          <Skeleton className="h-5 w-96" />
        </div>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i}>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-4 w-4 rounded" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-8 w-16 mb-2" />
                <Skeleton className="h-3 w-24" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return <ErrorPage message={error} onRetry={loadDashboard} />;
  }

  const data = dashboard || {
    totalReceitasMes: 0,
    totalDespesasMes: 0,
    saldoMes: 0,
    totalReceitasAno: 0,
    totalDespesasAno: 0,
    saldoAno: 0,
    fluxoCaixaMensal: [],
    receitasPorCategoria: [],
    despesasPorCategoria: [],
    ultimasMovimentacoes: [],
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">
          {t('menu.financeDashboard')}
        </h1>
        <p className="text-muted-foreground">
          {t('finance.dashboard.subtitle')}
        </p>
      </div>

      {/* Cards de Resumo */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('finance.dashboard.summary.monthRevenues', 'Receitas do Mês')}
            </CardTitle>
            <TrendingUp className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{formatCurrency(data.totalReceitasMes)}</div>
            <p className="text-xs text-muted-foreground">
              {t('finance.dashboard.summary.monthRevenuesHint', 'Total recebido este mês')}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('finance.dashboard.summary.monthExpenses', 'Despesas do Mês')}
            </CardTitle>
            <TrendingDown className="h-4 w-4 text-red-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">{formatCurrency(data.totalDespesasMes)}</div>
            <p className="text-xs text-muted-foreground">
              {t('finance.dashboard.summary.monthExpensesHint', 'Total pago este mês')}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('finance.dashboard.summary.monthBalance', 'Saldo do Mês')}
            </CardTitle>
            <DollarSign className="h-4 w-4" />
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${data.saldoMes >= 0 ? 'text-green-600' : 'text-red-600'}`}>
              {formatCurrency(data.saldoMes)}
            </div>
            <p className="text-xs text-muted-foreground">
              {t('finance.dashboard.summary.monthBalanceHint', 'Receitas - Despesas')}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('finance.dashboard.summary.yearBalance', 'Saldo do Ano')}
            </CardTitle>
            <Calendar className="h-4 w-4" />
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${data.saldoAno >= 0 ? 'text-green-600' : 'text-red-600'}`}>
              {formatCurrency(data.saldoAno)}
            </div>
            <p className="text-xs text-muted-foreground">
              {t(
                'finance.dashboard.summary.yearBalanceHint',
                `Receitas: ${formatCurrency(data.totalReceitasAno)} | Despesas: ${formatCurrency(data.totalDespesasAno)}`,
                {
                  revenues: formatCurrency(data.totalReceitasAno),
                  expenses: formatCurrency(data.totalDespesasAno),
                }
              )}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Fluxo de Caixa Mensal */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <BarChart3 className="h-5 w-5" />
            {t('finance.dashboard.cashFlow.title', 'Fluxo de Caixa - Últimos 12 Meses')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {data.fluxoCaixaMensal.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              {t('finance.dashboard.cashFlow.empty', 'Nenhum dado disponível')}
            </div>
          ) : (
            <div className="space-y-2">
              {data.fluxoCaixaMensal.map((item, idx) => (
                <div key={idx} className="flex items-center justify-between p-2 border rounded">
                  <div className="font-medium">{item.mesAno}</div>
                  <div className="flex items-center gap-4">
                    <span className="text-green-600">+{formatCurrency(item.totalReceitas)}</span>
                    <span className="text-red-600">-{formatCurrency(item.totalDespesas)}</span>
                    <span className={`font-bold ${item.saldo >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                      {formatCurrency(item.saldo)}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-4 md:grid-cols-2">
        {/* Receitas por Categoria */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <PieChart className="h-5 w-5" />
              {t('finance.dashboard.byCategory.revenuesTitle', 'Receitas por Categoria (Mês)')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            {data.receitasPorCategoria.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                {t('finance.dashboard.byCategory.revenuesEmpty', 'Nenhuma receita este mês')}
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('finance.dashboard.byCategory.table.category', 'Categoria')}</TableHead>
                    <TableHead className="text-right">
                      {t('finance.dashboard.byCategory.table.value', 'Valor')}
                    </TableHead>
                    <TableHead className="text-right">
                      {t('finance.dashboard.byCategory.table.percent', '%')}
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.receitasPorCategoria.map((item, idx) => (
                    <TableRow key={idx}>
                      <TableCell className="font-medium">{item.categoriaNome || t('finance.common.notCategorized')}</TableCell>
                      <TableCell className="text-right text-green-600">{formatCurrency(item.total)}</TableCell>
                      <TableCell className="text-right">{item.percentual.toFixed(1)}%</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>

        {/* Despesas por Categoria */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <PieChart className="h-5 w-5" />
              {t('finance.dashboard.byCategory.expensesTitle', 'Despesas por Categoria (Mês)')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            {data.despesasPorCategoria.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                {t('finance.dashboard.byCategory.expensesEmpty', 'Nenhuma despesa este mês')}
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('finance.dashboard.byCategory.table.category', 'Categoria')}</TableHead>
                    <TableHead className="text-right">
                      {t('finance.dashboard.byCategory.table.value', 'Valor')}
                    </TableHead>
                    <TableHead className="text-right">
                      {t('finance.dashboard.byCategory.table.percent', '%')}
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.despesasPorCategoria.map((item, idx) => (
                    <TableRow key={idx}>
                      <TableCell className="font-medium">{item.categoriaNome || t('finance.common.notCategorized')}</TableCell>
                      <TableCell className="text-right text-red-600">{formatCurrency(item.total)}</TableCell>
                      <TableCell className="text-right">{item.percentual.toFixed(1)}%</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Últimas Movimentações */}
      <Card>
        <CardHeader>
          <CardTitle>{t('finance.dashboard.lastMovements.title', 'Últimas Movimentações')}</CardTitle>
        </CardHeader>
        <CardContent>
          {data.ultimasMovimentacoes.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              {t('finance.dashboard.lastMovements.empty', 'Nenhuma movimentação recente')}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('finance.dashboard.lastMovements.table.type', 'Tipo')}</TableHead>
                  <TableHead>{t('finance.dashboard.lastMovements.table.description', 'Descrição')}</TableHead>
                  <TableHead>{t('finance.dashboard.lastMovements.table.date', 'Data')}</TableHead>
                  <TableHead>{t('finance.dashboard.lastMovements.table.status', 'Status')}</TableHead>
                  <TableHead className="text-right">
                    {t('finance.dashboard.lastMovements.table.value', 'Valor')}
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.ultimasMovimentacoes.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>
                      <span className={`px-2 py-1 rounded text-xs ${item.tipo === 'Receita' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                        {item.tipo === 'Receita'
                          ? t('finance.dashboard.lastMovements.type.revenue', 'Receita')
                          : t('finance.dashboard.lastMovements.type.expense', 'Despesa')}
                      </span>
                    </TableCell>
                    <TableCell className="font-medium">{item.descricao}</TableCell>
                    <TableCell>{formatDate(item.data)}</TableCell>
                    <TableCell>
                      <span className="px-2 py-1 rounded text-xs bg-gray-100 text-gray-800">{item.status}</span>
                    </TableCell>
                    <TableCell className={`text-right font-bold ${item.tipo === 'Receita' ? 'text-green-600' : 'text-red-600'}`}>
                      {item.tipo === 'Receita' ? '+' : '-'}{formatCurrency(item.valor)}
                    </TableCell>
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
