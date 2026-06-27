import { useEffect, useState } from 'react';
import { CalendarDays, Gift, Search } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { DataTablePagination } from '@/components/ui/data-table-pagination';
import { usePagination } from '@/hooks/usePagination';
import { pessoasApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

export default function Aniversariantes() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [dias, setDias] = useState('30');
  const [mes, setMes] = useState(''); // 1-12

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const diasNum = Number(dias) || 30;
      const mesNum = mes ? Number(mes) : null;
      const res = await pessoasApi.getAniversariantes(diasNum, 500, mesNum);
      setItems(res.data || []);
    } catch (err) {
      setError(t('birthdays.errorLoad'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const { page, pageSize, total, paginatedItems, setPage, setPageSize } = usePagination(items, 20);

  if (loading) return <LoadingPage text={t('birthdays.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('birthdays.title')}</h1>
          <p className="text-muted-foreground">{t('birthdays.subtitle')}</p>
        </div>
        <Button asChild variant="outline">
          <Link to="/pessoas/aniversariantes/campanha">
            <Gift className="h-4 w-4 mr-2" />
            Campanha no WhatsApp
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('birthdays.filter')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4 md:items-end">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2"><Search className="h-4 w-4" />{t('birthdays.days')}</label>
              <Input
                value={dias}
                onChange={(e) => setDias(e.target.value)}
                placeholder={t('birthdays.daysPlaceholder')}
                inputMode="numeric"
                disabled={!!mes}
              />
              {mes && (
                <p className="text-xs text-muted-foreground">
                  {t('birthdays.daysHint')}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('birthdays.month')}</label>
              <select
                value={mes || ''}
                onChange={(e) => setMes(e.target.value)}
                className="w-full px-3 py-2 bg-background border border-input rounded-lg focus:ring-2 focus:ring-ring focus:border-ring"
              >
                <option value="">{t('birthdays.allMonths')}</option>
                <option value="1">{t('birthdays.months.1')}</option>
                <option value="2">{t('birthdays.months.2')}</option>
                <option value="3">{t('birthdays.months.3')}</option>
                <option value="4">{t('birthdays.months.4')}</option>
                <option value="5">{t('birthdays.months.5')}</option>
                <option value="6">{t('birthdays.months.6')}</option>
                <option value="7">{t('birthdays.months.7')}</option>
                <option value="8">{t('birthdays.months.8')}</option>
                <option value="9">{t('birthdays.months.9')}</option>
                <option value="10">{t('birthdays.months.10')}</option>
                <option value="11">{t('birthdays.months.11')}</option>
                <option value="12">{t('birthdays.months.12')}</option>
              </select>
            </div>
            <div className="flex md:justify-end">
              <Button onClick={load} className="w-full md:w-auto">
                <CalendarDays className="h-4 w-4 mr-2" /> {t('birthdays.update')}
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('birthdays.listTitle')} ({total})</CardTitle>
        </CardHeader>
        <CardContent>
          {items.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">{t('birthdays.emptyMessage')}</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('birthdays.table.name')}</TableHead>
                  <TableHead>{t('birthdays.table.birthDate')}</TableHead>
                  <TableHead>{t('birthdays.table.nextBirthday')}</TableHead>
                  <TableHead>{t('birthdays.table.days')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((p) => (
                  <TableRow key={p.id}>
                    <TableCell className="font-medium">{p.nome}</TableCell>
                    <TableCell>{formatDate(p.dataNascimento)}</TableCell>
                    <TableCell>{formatDate(p.proximoAniversario)}</TableCell>
                    <TableCell>{p.diasParaAniversario}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
          {items.length > 0 && (
            <DataTablePagination
              page={page}
              pageSize={pageSize}
              total={total}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
