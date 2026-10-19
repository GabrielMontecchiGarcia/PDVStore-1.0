import { useCallback, useEffect, useRef, useState, type DependencyList } from 'react';
import { mensagemDeErro } from '../utils/format';

// ============================================================================
// Hook para carregar dados da API com estados de carregamento/erro e recarga.
// Centraliza o padrão usado por todas as páginas, evitando duplicação.
// ============================================================================

export function useApi<T>(carregar: () => Promise<T>, dependencias: DependencyList = []) {
  const [dados, setDados] = useState<T | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  const carregarRef = useRef(carregar);
  carregarRef.current = carregar;

  const recarregar = useCallback(async (silencioso = false) => {
    if (!silencioso) setCarregando(true);
    setErro(null);

    try {
      const resultado = await carregarRef.current();
      setDados(resultado);
      return resultado;
    } catch (e) {
      setErro(mensagemDeErro(e));
      return null;
    } finally {
      setCarregando(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, dependencias);

  useEffect(() => {
    void recarregar();
  }, [recarregar]);

  return { dados, carregando, erro, recarregar, setDados };
}
