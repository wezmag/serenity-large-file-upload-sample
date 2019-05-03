namespace Serenity {
    export namespace UploadHelper {
        export function addLargeFileUploadInput(options: UploadInputOptions): JQuery {
            options.container.addClass('fileinput-button');
            
            var uploadInput = $('<input/>').attr('type', 'file')
                .attr('name', options.inputName + '[]')
                .attr('data-url', Q.resolveUrl('~/api/fileupload/'))
                .attr('multiple', 'multiple')
                .appendTo(options.container);

            if (options.allowMultiple) {
                uploadInput.attr('multiple', 'multiple');
            }

            (uploadInput as any).fileupload({
                multipart: true,
                maxChunkSize: 4000000,
                dropZone: options.zone,
                pasteZone: options.zone,
                done: (e: JQueryEventObject, data: any) => {
                    var response = data.result;
                    if (options.fileDone != null) {
                        options.fileDone(response, data.files[0].name, data);
                    }
                },
                start: function () {
                    Q.blockUI(null);
                    if (options.progress != null) {
                        options.progress.show();
                    }
                },
                stop: function () {
                    Q.blockUndo();
                    if (options.progress != null) {
                        options.progress.hide();
                    }
                },
                progressall: (e1: JQueryEventObject, data1: any) => {
                    if (options.progress != null) {
                        var percent = data1.loaded / data1.total * 100;
                        options.progress.children().css('width', percent.toString() + '%');
                    }
                },
                submit: (e: JQueryEventObject, data: any) => {
                    var file = data.files[0];
                    data.headers = $.extend(data.headers, { 'X-File-Token': Q.Authorization.username });
                },
                fail: (e: JQueryEventObject, data: any) => {
                    Q.alert(data.jqXHR.responseJSON.Message);
                }
            });

            return uploadInput;
        }
    }
}