import { Injectable } from '@angular/core';
import { DailyWeatherPointDto, NextFiveDayForecastDto, TemperatureUnit } from '../models';

@Injectable({ providedIn: 'root' })
export class PdfExportService {
  private readonly page = {
    marginLeft: 40,
    marginRight: 40,
    contentWidth: 515
  };

  async exportCountryForecast(country: string, forecast: NextFiveDayForecastDto): Promise<void> {
    const { doc, autoTableFn } = await this.createDocument();
    this.writeCountrySection(doc, autoTableFn, country, forecast, true);
    doc.save(`forecast-${country.toLowerCase()}-${this.getDateStamp()}.pdf`);
  }

  async exportAllCountryForecasts(forecasts: Array<{ country: string; forecast: NextFiveDayForecastDto }>): Promise<void> {
    const { doc, autoTableFn } = await this.createDocument();

    forecasts.forEach((entry, index) => {
      this.writeCountrySection(doc, autoTableFn, entry.country, entry.forecast, index === 0);
      if (index < forecasts.length - 1) {
        doc.addPage();
      }
    });

    doc.save(`forecast-all-countries-${this.getDateStamp()}.pdf`);
  }

  private writeCountrySection(
    doc: any,
    autoTableFn: any,
    country: string,
    forecast: NextFiveDayForecastDto,
    isFirstSection: boolean
  ): void {
    if (!isFirstSection) {
      doc.setPage(doc.getNumberOfPages());
    }

    const days = [...forecast.days].sort((a, b) => new Date(a.dateUtc).getTime() - new Date(b.dateUtc).getTime());
    const tempUnit = this.getTempUnit(forecast.units);
    const windUnit = this.getWindUnit(forecast.units);

    this.drawHeader(doc, country, forecast);

    const barChartBottom = this.drawClusteredBarChart(
      doc,
      days,
      forecast.units,
      this.page.marginLeft,
      126,
      this.page.contentWidth,
      150
    );

    const lineChartBottom = this.drawLineChart(
      doc,
      days,
      forecast.units,
      this.page.marginLeft,
      barChartBottom + 20,
      this.page.contentWidth,
      150
    );

    const rows = days.map((day) => [
      this.formatDay(day.dateUtc),
      `${day.temperature.toFixed(1)} ${tempUnit}`,
      `${day.feelsLike.toFixed(1)} ${tempUnit}`,
      `${day.humidity}%`,
      `${day.windSpeed.toFixed(1)} ${windUnit}`,
      this.toTitleCase(day.summary)
    ]);

    autoTableFn(doc, {
      startY: lineChartBottom + 18,
      margin: { left: this.page.marginLeft, right: this.page.marginRight },
      head: [['Date', `Temperature (${tempUnit})`, `Feels Like (${tempUnit})`, 'Humidity', `Wind (${windUnit})`, 'Summary']],
      body: rows,
      theme: 'striped',
      headStyles: {
        fillColor: [31, 74, 102],
        textColor: [255, 255, 255],
        fontStyle: 'bold',
        halign: 'center'
      },
      bodyStyles: {
        textColor: [28, 54, 75],
        fontSize: 9.5,
        cellPadding: 4
      },
      alternateRowStyles: {
        fillColor: [247, 251, 255]
      },
      columnStyles: {
        0: { cellWidth: 85 },
        1: { halign: 'right', cellWidth: 85 },
        2: { halign: 'right', cellWidth: 85 },
        3: { halign: 'right', cellWidth: 70 },
        4: { halign: 'right', cellWidth: 75 },
        5: { cellWidth: 115 }
      },
      didDrawPage: () => {
        this.drawFooter(doc);
      }
    });
  }

  private drawHeader(doc: any, country: string, forecast: NextFiveDayForecastDto): void {
    doc.setTextColor(18, 47, 67);
    doc.setFontSize(17);
    doc.setFont('helvetica', 'bold');
    doc.text(`Country Forecast Report: ${country}`, this.page.marginLeft, 52);

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10.5);
    doc.setTextColor(68, 89, 104);
    doc.text(`City: ${forecast.city}, ${forecast.country}`, this.page.marginLeft, 73);
    doc.text(`Units: ${forecast.units}`, this.page.marginLeft + 180, 73);
    doc.text(`Generated: ${new Date().toLocaleString()}`, this.page.marginLeft + 280, 73);

    doc.setDrawColor(196, 210, 222);
    doc.line(this.page.marginLeft, 84, this.page.marginLeft + this.page.contentWidth, 84);
  }

  private drawFooter(doc: any): void {
    const pageHeight = doc.internal.pageSize.getHeight();
    doc.setFontSize(8);
    doc.setTextColor(118, 132, 144);
    doc.text('Weather Scope Export', this.page.marginLeft, pageHeight - 18);
    doc.text(`Page ${doc.getCurrentPageInfo().pageNumber}`, this.page.marginLeft + this.page.contentWidth - 35, pageHeight - 18);
  }

  private drawClusteredBarChart(
    doc: any,
    days: DailyWeatherPointDto[],
    units: TemperatureUnit,
    x: number,
    y: number,
    width: number,
    height: number
  ): number {
    this.drawChartTitle(doc, x, y - 8, 'Clustered Bar Chart', 'Each metric is normalized (0-100%) for comparison.');

    doc.setDrawColor(198, 210, 221);
    doc.setFillColor(252, 253, 255);
    doc.rect(x, y, width, height, 'FD');

    if (days.length === 0) {
      this.drawEmptyChartMessage(doc, x, y);
      return y + height;
    }

    const chartTop = y + 24;
    const chartBottom = y + height - 30;
    const chartLeft = x + 40;
    const chartRight = x + width - 10;
    const chartHeight = chartBottom - chartTop;
    const chartWidth = chartRight - chartLeft;

    const metrics = ['temperature', 'humidity', 'windSpeed'] as const;
    const colors: Record<(typeof metrics)[number], [number, number, number]> = {
      temperature: [224, 122, 47],
      humidity: [43, 140, 196],
      windSpeed: [74, 157, 91]
    };

    const maxByMetric = {
      temperature: Math.max(...days.map((d) => d.temperature), 1),
      humidity: Math.max(...days.map((d) => d.humidity), 1),
      windSpeed: Math.max(...days.map((d) => d.windSpeed), 1)
    };

    this.drawNormalizedYAxis(doc, chartLeft, chartRight, chartTop, chartBottom);

    const groupWidth = chartWidth / days.length;
    const barGap = Math.max(3, Math.min(8, groupWidth * 0.08));
    const barWidth = Math.max(8, Math.min(16, (groupWidth - barGap * 4) / 3));

    doc.setDrawColor(210, 220, 230);
    doc.line(chartLeft, chartBottom, chartRight, chartBottom);

    days.forEach((day, dayIndex) => {
      const centerX = chartLeft + groupWidth * dayIndex + groupWidth / 2;
      const clusterWidth = barWidth * 3 + barGap * 2;
      const startX = centerX - clusterWidth / 2;

      metrics.forEach((metric, metricIndex) => {
        const rawValue = this.getMetricValue(day, metric);
        const maxValue = maxByMetric[metric];
        const normalized = maxValue === 0 ? 0 : rawValue / maxValue;
        const barHeight = normalized * chartHeight;
        const barX = startX + metricIndex * (barWidth + barGap);
        const barY = chartBottom - barHeight;
        const color = colors[metric];

        doc.setFillColor(color[0], color[1], color[2]);
        doc.rect(barX, barY, barWidth, barHeight, 'F');
      });

      doc.setFontSize(8);
      doc.setTextColor(75, 97, 114);
      doc.text(this.formatShortDay(day.dateUtc), centerX - 9, chartBottom + 12);
    });

    doc.setFontSize(8);
    this.drawLegendItem(doc, x + 12, y + height - 10, [224, 122, 47], `Temperature (${this.getTempUnit(units)})`);
    this.drawLegendItem(doc, x + 165, y + height - 10, [43, 140, 196], 'Humidity (%)');
    this.drawLegendItem(doc, x + 282, y + height - 10, [74, 157, 91], `Wind (${this.getWindUnit(units)})`);

    return y + height;
  }

  private drawLineChart(
    doc: any,
    days: DailyWeatherPointDto[],
    units: TemperatureUnit,
    x: number,
    y: number,
    width: number,
    height: number
  ): number {
    this.drawChartTitle(doc, x, y - 8, 'Line Chart', 'Trend view, normalized per metric (0-100%).');

    doc.setDrawColor(198, 210, 221);
    doc.setFillColor(252, 253, 255);
    doc.rect(x, y, width, height, 'FD');

    if (days.length === 0) {
      this.drawEmptyChartMessage(doc, x, y);
      return y + height;
    }

    const chartTop = y + 24;
    const chartBottom = y + height - 30;
    const chartLeft = x + 40;
    const chartRight = x + width - 10;
    const chartHeight = chartBottom - chartTop;
    const chartWidth = chartRight - chartLeft;
    const pointsCount = days.length;
    const xStep = pointsCount > 1 ? chartWidth / (pointsCount - 1) : 0;

    const metrics = ['temperature', 'humidity', 'windSpeed'] as const;
    const colors: Record<(typeof metrics)[number], [number, number, number]> = {
      temperature: [224, 122, 47],
      humidity: [43, 140, 196],
      windSpeed: [74, 157, 91]
    };

    const maxByMetric = {
      temperature: Math.max(...days.map((d) => d.temperature), 1),
      humidity: Math.max(...days.map((d) => d.humidity), 1),
      windSpeed: Math.max(...days.map((d) => d.windSpeed), 1)
    };

    this.drawNormalizedYAxis(doc, chartLeft, chartRight, chartTop, chartBottom);
    doc.setDrawColor(210, 220, 230);
    doc.line(chartLeft, chartBottom, chartRight, chartBottom);

    metrics.forEach((metric) => {
      const color = colors[metric];
      doc.setDrawColor(color[0], color[1], color[2]);
      doc.setLineWidth(1.35);

      let lastX = chartLeft;
      let lastY = chartBottom;

      days.forEach((day, index) => {
        const value = this.getMetricValue(day, metric);
        const normalized = maxByMetric[metric] === 0 ? 0 : value / maxByMetric[metric];
        const pointX = chartLeft + xStep * index;
        const pointY = chartBottom - normalized * chartHeight;

        if (index > 0) {
          doc.line(lastX, lastY, pointX, pointY);
        }

        doc.setFillColor(color[0], color[1], color[2]);
        doc.circle(pointX, pointY, 1.8, 'F');
        lastX = pointX;
        lastY = pointY;
      });
    });

    days.forEach((day, dayIndex) => {
      const pointX = chartLeft + xStep * dayIndex;
      doc.setFontSize(8);
      doc.setTextColor(75, 97, 114);
      doc.text(this.formatShortDay(day.dateUtc), pointX - 9, chartBottom + 12);
    });

    doc.setFontSize(8);
    this.drawLegendItem(doc, x + 12, y + height - 10, [224, 122, 47], `Temperature (${this.getTempUnit(units)})`);
    this.drawLegendItem(doc, x + 165, y + height - 10, [43, 140, 196], 'Humidity (%)');
    this.drawLegendItem(doc, x + 282, y + height - 10, [74, 157, 91], `Wind (${this.getWindUnit(units)})`);

    return y + height;
  }

  private drawChartTitle(doc: any, x: number, y: number, title: string, subtitle: string): void {
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10.5);
    doc.setTextColor(34, 61, 82);
    doc.text(title, x, y);
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(8.2);
    doc.setTextColor(92, 110, 123);
    doc.text(subtitle, x + 118, y);
  }

  private drawEmptyChartMessage(doc: any, x: number, y: number): void {
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9.5);
    doc.setTextColor(95, 110, 120);
    doc.text('No chart data available.', x + 12, y + 22);
  }

  private drawNormalizedYAxis(doc: any, left: number, right: number, top: number, bottom: number): void {
    const ticks = [0, 25, 50, 75, 100];

    ticks.forEach((tick) => {
      const ratio = tick / 100;
      const y = bottom - ratio * (bottom - top);
      doc.setDrawColor(225, 233, 240);
      doc.setLineWidth(0.4);
      doc.line(left, y, right, y);
      doc.setTextColor(120, 133, 144);
      doc.setFontSize(7.5);
      doc.text(`${tick}%`, left - 20, y + 2.5);
    });
  }

  private drawLegendItem(doc: any, x: number, y: number, color: [number, number, number], label: string): void {
    doc.setFillColor(color[0], color[1], color[2]);
    doc.rect(x, y - 7, 10, 6, 'F');
    doc.setTextColor(52, 70, 86);
    doc.setFont('helvetica', 'normal');
    doc.text(label, x + 14, y - 2);
  }

  private getMetricValue(day: DailyWeatherPointDto, metric: 'temperature' | 'humidity' | 'windSpeed'): number {
    if (metric === 'temperature') {
      return day.temperature;
    }

    if (metric === 'humidity') {
      return day.humidity;
    }

    return day.windSpeed;
  }

  private formatDay(dateValue: string): string {
    return new Date(dateValue).toLocaleDateString(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    });
  }

  private formatShortDay(dateValue: string): string {
    return new Date(dateValue).toLocaleDateString(undefined, { weekday: 'short' });
  }

  private toTitleCase(value: string): string {
    return value
      .split(' ')
      .filter((part) => part.length > 0)
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1).toLowerCase())
      .join(' ');
  }

  private getTempUnit(units: TemperatureUnit): string {
    return units === 'Imperial' ? 'F' : 'C';
  }

  private getWindUnit(units: TemperatureUnit): string {
    return units === 'Imperial' ? 'mph' : 'm/s';
  }

  private getDateStamp(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private async createDocument(): Promise<{ doc: any; autoTableFn: any }> {
    const [{ default: JsPdf }, { default: autoTableFn }] = await Promise.all([
      import('jspdf'),
      import('jspdf-autotable')
    ]);

    return {
      doc: new JsPdf({ unit: 'pt', format: 'a4' }),
      autoTableFn
    };
  }
}
