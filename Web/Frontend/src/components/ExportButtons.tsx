import { useState } from 'react';
import { relatorioService } from '../services/relatorioService';
import { baixarArquivo, sufixoDataHora } from '../utils/download';
import { useToast } from '../contexts/ToastContext';
import { mensagemDeErro } from '../utils/format';
import { Icon } from './Icon';

// ============================================================================
// Botões de exportação em Excel/PDF. Reutilizados pelo dashboard e cadastros,
// equivalente aos botões "Exportar PDF/Excel" do sistema Windows.
// ============================================================================

interface ExportButtonsProps {
  tipo: string;
  params?: { inicio?: string; fim?: string; apenasAtivos?: boolean };
  rotulo?: string;
}

export function ExportButtons({ tipo, params = {}, rotulo = 'Exportar' }: ExportButtonsProps) {
  const [formatoAtual, setFormatoAtual] = useState<'excel' | 'pdf' | null>(null);
  const { sucesso, erro } = useToast();

  const exportar = async (formato: 'excel' | 'pdf') => {
    setFormatoAtual(formato);
    try {
      const blob = await relatorioService.exportar(tipo, formato, params);
      const extensao = formato === 'pdf' ? 'pdf' : 'xlsx';
      baixarArquivo(blob, `${tipo}_${sufixoDataHora()}.${extensao}`);
      sucesso(`Arquivo ${extensao.toUpperCase()} gerado com sucesso.`);
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setFormatoAtual(null);
    }
  };

  return (
    <div className="export-buttons" title={rotulo}>
      <button
        type="button"
        className="btn btn-outline btn-sm"
        onClick={() => exportar('excel')}
        disabled={formatoAtual !== null}
      >
        <Icon name="download" size={16} />
        {formatoAtual === 'excel' ? 'Gerando...' : 'Excel'}
      </button>
      <button
        type="button"
        className="btn btn-outline btn-sm"
        onClick={() => exportar('pdf')}
        disabled={formatoAtual !== null}
      >
        <Icon name="download" size={16} />
        {formatoAtual === 'pdf' ? 'Gerando...' : 'PDF'}
      </button>
    </div>
  );
}
