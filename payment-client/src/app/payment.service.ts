import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { CardPaymentResponse, NewCardModel } from './models/payment.models';
import { environment } from '../environments/environment';
import io from 'socket.io-client';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private httpClient = inject(HttpClient);

  tokenizeCard(model: NewCardModel){
    return this.httpClient.post<CardPaymentResponse>(`${environment.API_URL}/api/payment`, model);
  }
  queryTransaction(payRequestId: string) {
    return this.httpClient.get(`${environment.API_URL}/api/payment/query/${payRequestId}`);
  }
  getVaultedCard(vaultId: string) {
    return this.httpClient.get(`${environment.API_URL}/api/payment/${vaultId}`);
  }
  openSocketConnection(payRequestId: string) {
    return io(environment.SOCKET_URL, {
      query: {
        PAYREQUESTID: payRequestId
      }
    });
  }
}
