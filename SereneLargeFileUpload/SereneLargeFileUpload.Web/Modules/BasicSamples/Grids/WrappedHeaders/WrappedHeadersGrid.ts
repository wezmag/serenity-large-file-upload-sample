/// <reference path="../../../Northwind/Order/OrderGrid.ts" />

namespace SereneLargeFileUpload.BasicSamples {

    @Serenity.Decorators.registerClass()
    export class WrappedHeadersGrid extends Northwind.OrderGrid {

        constructor(container: JQuery) {
            super(container);
        }
    }
}