﻿import { Component, Injector, ViewEncapsulation } from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import { MenuItem } from '@shared/layout/menu-item';

@Component({
    templateUrl: './sidebar-nav.component.html',
    selector: 'sidebar-nav',
    encapsulation: ViewEncapsulation.None
})
export class SideBarNavComponent extends AppComponentBase {

    menuItems: MenuItem[] = [
        new MenuItem(this.l("HomePage"), "", "home", "/app/home"),
        new MenuItem(this.l("Tenants"), "System.Administration.Tenants", "business", "/app/tenants"),
        new MenuItem(this.l("Users"), "System.Administration.Users", "people", "/app/users"),
        new MenuItem(this.l("About"), "", "info", "/app/about"),

        new MenuItem(this.l("MultiLevelMenu"), "", "menu", "", [
            new MenuItem("ASP.NET Boilerplate", "", "", "", [
                new MenuItem("Home", "", "", "https://aspnetboilerplate.com"),
                new MenuItem("Templates", "", "", "https://aspnetboilerplate.com/Templates"),
                new MenuItem("Samples", "", "", "https://aspnetboilerplate.com/Samples"),
                new MenuItem("Documents", "", "", "https://aspnetboilerplate.com/Pages/Documents"),
                new MenuItem("Forum", "", "", "https://forum.aspnetboilerplate.com/"),
                new MenuItem("About", "", "", "https://aspnetboilerplate.com/About")
            ]),
            new MenuItem("ASP.NET Zero", "", "", "", [
                new MenuItem("Home", "", "", "https://aspnetzero.com?ref=studioxtmpl"),
                new MenuItem("Description", "", "", "https://aspnetzero.com/?ref=studioxtmpl#description"),
                new MenuItem("Features", "", "", "https://aspnetzero.com/?ref=studioxtmpl#features"),
                new MenuItem("Pricing", "", "", "https://aspnetzero.com/?ref=studioxtmpl#pricing"),
                new MenuItem("Faq", "", "", "https://aspnetzero.com/Faq?ref=studioxtmpl"),
                new MenuItem("Documents", "", "", "https://aspnetzero.com/Documents?ref=studioxtmpl")
            ])
        ])
    ];

    constructor(
        injector: Injector
    ) {
        super(injector);
    }

    showMenuItem(menuItem): boolean {
        if (menuItem.permissionName) {
            return this.permission.isGranted(menuItem.permissionName);
        }

        return true;
    }
}