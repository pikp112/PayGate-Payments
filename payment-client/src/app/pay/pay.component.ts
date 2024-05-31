import { Component, inject } from '@angular/core';
import { NewCardModel } from '../models/payment.models';
import { PaymentService } from '../payment.service';
import { DomSanitizer } from '@angular/platform-browser';
import { NgForm } from '@angular/forms';
import { Socket } from 'socket.io-client';

@Component({
  selector: 'app-pay',
  templateUrl: './pay.component.html',
  styleUrl: './pay.component.css'
})
export class PayComponent {
  model = {} as NewCardModel;
  socket: Socket;
  Secure3DHTML: any;
  payRequestId: string;
  paymentResult: any;
  isLoading = false;

  private paymentService = inject(PaymentService);
  private sanitizer = inject(DomSanitizer);

  connecToSocket(paymentId: string) {
    this.socket = this.paymentService.openSocketConnection(paymentId);
    this.socket.on('message', (e:any) => { console.log('connected to socket')});
    this.socket.on('joined', (content: any) => { console.log('joined', content) });
    this.socket.on('complete', async (payload: any) => {
      this.Secure3DHTML = null;
      this.completeFollowUp();
    })
  }

  completeFollowUp() {
    this.isLoading = true;
    this.paymentService.queryTransaction(this.payRequestId)
      .toPromise()
      .then(async (response: any) => {
        this.isLoading = false;
        this.paymentResult = response;
      }, async (error: any) => {
        this.isLoading = false;
        console.log(error);
      });
  }

  makePayment(form: NgForm) {
    if (form.submitted && form.valid){
      this.isLoading = true;
      this.paymentService.tokenizeCard(form.value)
        .toPromise()
        .then((response: any) => {
          this.isLoading = false;
          if (response.completed){
            response.response = JSON.parse(response.response);
            this.paymentResult = response;
          } else {
            // 3D secure?
            this.Secure3DHTML = this.sanitizer.bypassSecurityTrustResourceUrl(`data:text/html,${response.secure3DHtml}`);
            this.payRequestId = response.payRequestId;
            this.connecToSocket(response.payRequestId);
          }
        }).catch((error: any) => {
          this.isLoading = false;
          console.log(error);
        });
    }
  }
}
