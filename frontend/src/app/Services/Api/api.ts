
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { env } from '../../../env';

@Injectable({
  providedIn: 'root'
})
export class Api {
  url: string;

  constructor(private http: HttpClient) {
    this.url = env.API_URL;
    console.log('API URL:', this.url);
  }

  StartCrawl(url: string, mode: string) {
    const params = { url: url, mode: mode };
    return this.http.post(`${this.url}/api/crawler/crawl-url`, null, { params });
  }
}
