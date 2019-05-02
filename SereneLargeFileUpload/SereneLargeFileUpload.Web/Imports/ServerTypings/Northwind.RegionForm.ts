namespace SereneLargeFileUpload.Northwind {
    export interface RegionForm {
        RegionID: Serenity.IntegerEditor;
        RegionDescription: Serenity.StringEditor;
        FileUpload1: Serenity.MultipleImageUploadEditor;
        FileUpload2: LargeFileUploadEditor;
    }

    export class RegionForm extends Serenity.PrefixedContext {
        static formKey = 'Northwind.Region';
        private static init: boolean;

        constructor(prefix: string) {
            super(prefix);

            if (!RegionForm.init)  {
                RegionForm.init = true;

                var s = Serenity;
                var w0 = s.IntegerEditor;
                var w1 = s.StringEditor;
                var w2 = s.MultipleImageUploadEditor;
                var w3 = LargeFileUploadEditor;

                Q.initFormType(RegionForm, [
                    'RegionID', w0,
                    'RegionDescription', w1,
                    'FileUpload1', w2,
                    'FileUpload2', w3
                ]);
            }
        }
    }
}

