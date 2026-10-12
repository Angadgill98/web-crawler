import { Component, inject, input, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Api } from './Services/Api/api';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  api:Api=inject(Api)
  url = signal<string>('');
  mode = signal<string>('http');

  CrawlUrl(url: string, mode: string) {
    if (!this.IsValidUrl(url)) {
      console.error('Invalid URL');
      return;
    }

    if (mode !== 'http' && mode !== 'js') {
      console.error('Invalid mode. Allowed modes: http, js');
      return;
    }

    this.api.StartCrawl(url, mode).subscribe({
      next: (response) => {
        console.log('Crawl succeeded:', response);
        this.DownloadJson(response, 'crawl-results.json');
      },
      error: (error) => {
        console.error('Crawl failed:', error);
      }
    });
  }

  IsValidUrl(url: string): boolean {
    try {
      const parsedUrl = new URL(url);
      return parsedUrl.protocol === 'http:' || parsedUrl.protocol === 'https:';
    } catch {
      return false;
    }
  }

  DownloadJson(data: unknown, filename: string) {
    const json = JSON.stringify(data, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const downloadUrl = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = downloadUrl;
    link.download = filename;
    link.click();

    URL.revokeObjectURL(downloadUrl);
  }
}
