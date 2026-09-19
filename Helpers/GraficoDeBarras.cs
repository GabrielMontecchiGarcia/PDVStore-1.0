using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PDVStore.Helpers
{
    /// <summary>
    /// Desenha gráficos de barras simples (agrupadas) usando GDI+.
    /// </summary>
    public static class GraficoDeBarras
    {
        public sealed class Serie
        {
            public string Nome { get; set; } = "";
            public Color Cor { get; set; }
            public decimal[] Valores { get; set; } = Array.Empty<decimal>();
        }

        public static void Desenhar(Graphics g, Rectangle bounds, string titulo, string[] rotulos, List<Serie> series)
        {
            g.FillRectangle(Brushes.White, bounds);

            using var fonteTitulo = new Font("Segoe UI", 11F, FontStyle.Bold);
            g.DrawString(titulo, fonteTitulo, Brushes.Black, bounds.X + 8, bounds.Y + 4);

            if (rotulos.Length == 0 ||
                series.Count == 0 ||
                series.All(s => s.Valores.Length == 0))
            {
                using var fonteAvviso = new Font("Segoe UI", 10F);
                g.DrawString("Sem dados no período selecionado.", fonteAvviso, Brushes.DimGray, bounds.X + 10, bounds.Y + 42);
                return;
            }

            const int margemInferior = 32;
            const int margemTopo = 46;
            const int margemLateral = 12;

            var area = new Rectangle(
                bounds.X + margemLateral,
                bounds.Y + margemTopo,
                bounds.Width - margemLateral * 2,
                bounds.Height - margemTopo - margemInferior);

            decimal max = 0;
            foreach (var s in series)
                foreach (var v in s.Valores)
                    if (v > max) max = v;

            if (max <= 0) max = 1;

            int grupos = series.Count;
            float slot = area.Width / (float)Math.Max(1, rotulos.Length);
            float barra = Math.Min(slot * 0.55f / grupos, 42f);

            using var fonteValor = new Font("Segoe UI", 7F);
            using var fonteRotulo = new Font("Segoe UI", 8F);
            using var fonteLegenda = new Font("Segoe UI", 9F);

            // Legenda
            int lx = bounds.X + 8;
            int ly = bounds.Y + 24;
            foreach (var s in series)
            {
                using var brush = new SolidBrush(s.Cor);
                g.FillRectangle(brush, lx, ly + 2, 12, 12);
                g.DrawRectangle(Pens.Black, lx, ly + 2, 12, 12);
                g.DrawString(s.Nome, fonteLegenda, Brushes.Black, lx + 14, ly);
                lx += 14 + (int)g.MeasureString(s.Nome, fonteLegenda).Width + 18;
            }

            int passoRotulo = rotulos.Length <= 14 ? 1 : (int)Math.Ceiling(rotulos.Length / 14.0);

            for (int i = 0; i < rotulos.Length; i++)
            {
                float x0 = area.X + i * slot + slot / 2 - (barra * grupos) / 2;

                for (int sIdx = 0; sIdx < series.Count; sIdx++)
                {
                    int h = (int)Math.Round((double)series[sIdx].Valores[i] / (double)max * area.Height);
                    var rect = new Rectangle((int)(x0 + sIdx * barra), area.Bottom - h, (int)barra, h);

                    using var brush = new SolidBrush(series[sIdx].Cor);
                    g.FillRectangle(brush, rect);

                    if (rect.Height > 0)
                        g.DrawString(series[sIdx].Valores[i].ToString("0.##"), fonteValor, Brushes.Black,
                            rect.X, rect.Y - 13);
                }

                if (i % passoRotulo == 0)
                {
                    var texto = rotulos[i];
                    var medida = g.MeasureString(texto, fonteRotulo);
                    g.DrawString(texto, fonteRotulo, Brushes.Black,
                        Math.Max(area.X, x0 - medida.Width / 2),
                        area.Bottom + 6);
                }
            }

            g.DrawLine(Pens.DimGray, area.X, area.Bottom, area.Right, area.Bottom);
        }
    }
}