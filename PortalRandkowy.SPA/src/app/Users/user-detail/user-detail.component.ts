import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { User } from 'src/app/_models/user';
import { AlertifyService } from 'src/app/_services/alertify.service';
import { UserService } from 'src/app/_services/user.service';
import { NgxGalleryOptions, NgxGalleryImage, NgxGalleryAnimation } from '@kolkov/ngx-gallery';
import { TabsetComponent } from 'ngx-bootstrap/tabs';

@Component({
  selector: 'app-user-detail',
  templateUrl: './user-detail.component.html',
  styleUrls: ['./user-detail.component.scss']
})
export class UserDetailComponent implements OnInit {
  
  @ViewChild('userTabs', { static: true }) userTabs: TabsetComponent;

  user: User;
  galleryOptions: NgxGalleryOptions[];
  galleryImages: NgxGalleryImage[];

  constructor(private userServices: UserService,
              private alertify: AlertifyService,
              private route: ActivatedRoute){}



  ngOnInit(): void {
    this.route.data.subscribe(data => {
      this.user = data.user;
    });

    this.route.queryParams.subscribe(params => {
      const selectTab = params.tab;
      this.userTabs.tabs[selectTab > 0 ? selectTab : 0].active = true;
    })

    this.galleryOptions = [
      {
          width: '500px',
          height: '500px',
          thumbnailsColumns: 4,
          imagePercent: 100,
          preview: false,
          imageAnimation: NgxGalleryAnimation.Slide
      }
  ];

    this.galleryImages = this.getImages();
  }

  getImages(): any{
    const imagesUrl = [];
    // tslint:disable-next-line: prefer-for-of
    for (let i = 0; i < this.user.photos.length; i++){
      imagesUrl.push({
        small: this.user.photos[i].url,
        medium: this.user.photos[i].url,
        big: this.user.photos[i].url,
        description: this.user.photos[i].description
      });
    }
    return imagesUrl;
  }

  selectTab(tabId: number){
    this.userTabs.tabs[tabId].active = true;
  }


}
