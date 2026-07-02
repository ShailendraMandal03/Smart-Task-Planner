import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  public apiUrl = 'http://localhost:5000/api'; // Standard default port, might need adjustment

  public get<T>(path: string): Observable<T> {
    return this.http.get<T>(`${this.apiUrl}${path}`).pipe(catchError(this.handleError));
  }

  public post<T>(path: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.apiUrl}${path}`, body).pipe(catchError(this.handleError));
  }

  public put<T>(path: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.apiUrl}${path}`, body).pipe(catchError(this.handleError));
  }

  public delete<T>(path: string): Observable<T> {
    return this.http.delete<T>(`${this.apiUrl}${path}`).pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse) {
    let errorMessage = 'An unknown error occurred!';
    if (error.error instanceof ErrorEvent) {
      // Client-side errors
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side errors, expecting ProblemDetails
      if (error.error && error.error.detail) {
         errorMessage = error.error.detail;
      } else {
         errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
      }
    }
    return throwError(() => new Error(errorMessage));
  }
}
