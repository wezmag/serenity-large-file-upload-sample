namespace SereneLargeFileUpload.Northwind {
    export interface RegionRow {
        RegionID?: number;
        RegionDescription?: string;
        FileUpload1?: string;
        FileUpload2?: string;
    }

    export namespace RegionRow {
        export const idProperty = 'RegionID';
        export const nameProperty = 'RegionDescription';
        export const localTextPrefix = 'Northwind.Region';
        export const lookupKey = 'Northwind.Region';

        export function getLookup(): Q.Lookup<RegionRow> {
            return Q.getLookup<RegionRow>('Northwind.Region');
        }

        export declare const enum Fields {
            RegionID = "RegionID",
            RegionDescription = "RegionDescription",
            FileUpload1 = "FileUpload1",
            FileUpload2 = "FileUpload2"
        }
    }
}

