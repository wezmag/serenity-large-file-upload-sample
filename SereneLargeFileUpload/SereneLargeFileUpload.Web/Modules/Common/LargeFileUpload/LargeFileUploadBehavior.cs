using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Serenity;
using Serenity.Data;
using Serenity.Services;
using Serenity.Web;

namespace SereneLargeFileUpload.Common.LargeFileUpload
{
    public class LargeFileUploadBehavior : BaseSaveDeleteBehavior, IImplicitBehavior, IFieldBehavior
    {
        public Field Target { get; set; }

        private LargeFileUploadEditorAttribute attr;
        private string fileNameFormat;
        private const string SplittedFormat = "{1:00000}/{0:00000000}_{2}";
        private UploadHelper uploadHelper;
        private Dictionary<string, Field> replaceFields;

        public bool ActivateFor(Row row)
        {
            if (ReferenceEquals(null, Target))
                return false;

            attr = Target.GetAttribute<LargeFileUploadEditorAttribute>();
            if (attr == null)
                return false;

            if (!(Target is StringField))
                throw new ArgumentException(String.Format(
                    "Field '{0}' on row type '{1}' has a LargeFileUploadEditor attribute but it is not a String field!",
                        Target.PropertyName ?? Target.Name, row.GetType().FullName));

            if (!(row is IIdRow))
                throw new ArgumentException(String.Format(
                    "Field '{0}' on row type '{1}' has a LargeFileUploadEditor attribute but Row type doesn't implement IIdRow!",
                        Target.PropertyName ?? Target.Name, row.GetType().FullName));

            var format = attr.FilenameFormat;

            if (format == null)
            {
                format = row.GetType().Name;
                if (format.EndsWith("Row"))
                    format = format.Substring(0, format.Length - 3);
                format += "/~";
            }

            this.fileNameFormat = format.Replace("~", SplittedFormat);
            this.replaceFields = ParseReplaceFields(this.fileNameFormat, row, Target);

            this.uploadHelper = new UploadHelper((attr.SubFolder.IsEmptyOrNull() ? "" : (attr.SubFolder + "/")) + (this.fileNameFormat));

            return true;
        }

        internal static Dictionary<string, Field> ParseReplaceFields(string fileNameFormat, Row row, Field target)
        {
            if (fileNameFormat.IndexOf('|') < 0)
                return null;

            var replaceFields = new Dictionary<string, Field>();

            int start = 0;
            while ((start = fileNameFormat.IndexOf('|', start)) >= 0)
            {
                var end = fileNameFormat.IndexOf('|', start + 1);
                if (end <= start + 1)
                    throw new ArgumentException(String.Format(
                        "Field '{0}' on row type '{1}' has a UploadEditor attribute " +
                        "with invalid format string '{2}'!",
                            target.PropertyName ?? target.Name,
                            row.GetType().FullName,
                            fileNameFormat));

                var fieldName = fileNameFormat.Substring(start + 1, end - start - 1);
                var actualName = fieldName;
                var colon = fieldName.IndexOf(":");
                if (colon >= 0)
                    actualName = fieldName.Substring(0, colon);

                var replaceField = row.FindFieldByPropertyName(actualName) ??
                    row.FindField(actualName);

                if (ReferenceEquals(null, replaceField))
                {
                    throw new ArgumentException(String.Format(
                        "Field '{0}' on row type '{1}' has a UploadEditor attribute that " +
                        "references field '{2}', but no such field is found!'",
                            target.PropertyName ?? target.Name,
                            row.GetType().FullName,
                            actualName));
                }

                replaceFields['|' + fieldName + '|'] = replaceField;

                start = end + 1;
            }

            return replaceFields;
        }

        private UploadedFile[] ParseAndValidate(string json, string key)
        {
            json = json.TrimToNull();

            if (json != null && (!json.StartsWith("[") || !json.EndsWith("]")))
                throw new ArgumentOutOfRangeException(key);

            var list = JSON.Parse<UploadedFile[]>(json ?? "[]");

            if (list.Any(x => string.IsNullOrEmpty(x.Filename)) ||
                list.GroupBy(x => x.Filename.Trim()).SelectMany(x => x.Skip(1)).Any())
                throw new ArgumentOutOfRangeException(key);

            return list;
        }

        public override void OnPrepareQuery(ISaveRequestHandler handler, SqlQuery query)
        {
            base.OnPrepareQuery(handler, query);

            if (this.replaceFields != null)
            {
                foreach (var field in replaceFields.Values)
                {
                    if (!field.IsTableField() &&
                        (!(query is ISqlQueryExtensible) ||
                          ((ISqlQueryExtensible)query).GetSelectIntoIndex(field) <= 0))
                        query.Select(field);
                }
            }
        }

        public override void OnBeforeSave(ISaveRequestHandler handler)
        {
            var field = (StringField)Target;

            if (!handler.Row.IsAssigned(field))
                return;

            var oldFilesJSON = (handler.IsCreate ? null : field[handler.Old]).TrimToNull();
            var newFilesJSON = field[handler.Row] = field[handler.Row].TrimToNull();

            if (oldFilesJSON.IsTrimmedSame(newFilesJSON))
            {
                field[handler.Row] = oldFilesJSON;
                return;
            }

            var oldFileList = ParseAndValidate(oldFilesJSON, "oldFiles");
            var newFileList = ParseAndValidate(newFilesJSON, "newFiles");

            var filesToDelete = new FilesToDelete();
            UploadHelper.RegisterFilesToDelete(handler.UnitOfWork, filesToDelete);
            handler.StateBag[this.GetType().FullName + "_" + Target.Name + "_FilesToDelete"] = filesToDelete;

            foreach (var file in oldFileList)
            {
                var filename = file.Filename.Trim();
                if (newFileList.Any(x => String.Compare(x.Filename.Trim(), filename, StringComparison.OrdinalIgnoreCase) == 0))
                    continue;

                DeleteOldFile(filesToDelete, filename);
            }

            if (newFileList.IsEmptyOrNull())
            {
                field[handler.Row] = null;
                return;
            }

            if (handler.IsUpdate)
                field[handler.Row] = CopyTemporaryFiles(handler, oldFileList, newFileList, filesToDelete);
        }

        internal static string ProcessReplaceFields(string s, Dictionary<string, Field> replaceFields, ISaveRequestHandler handler)
        {
            if (replaceFields == null)
                return s;

            var row = handler.Row;

            // foreign / calculated fields might not be available yet in new row
            // so load them from database 

            // TODO: if referenced foreign fields changed on update, 
            // values might be wrong in before update where we set filename
            // so need to handle update in AfterSave just like create

            if (handler.IsCreate &&
                replaceFields.Values.Any(x => !x.IsTableField()))
            {
                var idField = (Field)(((IIdRow)handler.Row).IdField);

                row = handler.Row.Clone();
                var query = new SqlQuery()
                    .From(row);

                foreach (var field in replaceFields.Values)
                    query.Select(field);

                query.Where(idField == new ValueCriteria(idField.AsObject(row)));

                query.GetFirst(handler.Connection);
            }

            foreach (var p in replaceFields)
            {
                var val = p.Value.AsObject(row);
                string str;

                var colon = p.Key.IndexOf(":");
                if (colon >= 0)
                    str = String.Format("{0:" + p.Key.Substring(colon + 1, p.Key.Length - colon - 2) + "}", val);
                else
                    str = Convert.ToString(val ?? "", CultureInfo.InvariantCulture);

                str = StringHelper.SanitizeFilename(str).Replace('\\', '_').Replace("..", "_");
                if (string.IsNullOrWhiteSpace(str))
                    str = "_";

                while (str.EndsWith("."))
                    str = str.Substring(0, str.Length - 1) + "_";

                s = s.Replace(p.Key, str);
            }

            while (s.IndexOf("//") > 0)
                s = s.Replace("//", "/_/");

            return s;
        }

        private void DeleteOldFile(FilesToDelete filesToDelete, string oldFilename)
        {
            if (!oldFilename.IsEmptyOrNull())
            {
                var actualOldFile = (attr.SubFolder.IsEmptyOrNull() ? "" : (attr.SubFolder + "/")) + oldFilename;
                filesToDelete.RegisterOldFile(actualOldFile);

                if (attr.CopyToHistory)
                {
                    var oldFilePath = UploadHelper.ToPath(actualOldFile);
                    string date = DateTime.UtcNow.ToString("yyyyMM", Invariants.DateTimeFormat);
                    string historyFile = "history/" + date + "/" + Path.GetFileName(oldFilePath);
                    if (File.Exists(UploadHelper.DbFilePath(oldFilePath)))
                        UploadHelper.CopyFileAndRelated(UploadHelper.DbFilePath(oldFilePath), UploadHelper.DbFilePath(historyFile), overwrite: true);
                }
            }
        }

        public override void OnAfterDelete(IDeleteRequestHandler handler)
        {
            if (ServiceQueryHelper.UseSoftDelete(handler.Row))
                return;

            var field = (StringField)Target;
            var oldFilesJSON = field[handler.Row].TrimToNull();
            var oldFileList = ParseAndValidate(oldFilesJSON, "oldFiles");

            var filesToDelete = new FilesToDelete();
            UploadHelper.RegisterFilesToDelete(handler.UnitOfWork, filesToDelete);

            foreach (var file in oldFileList)
                DeleteOldFile(filesToDelete, file.Filename);
        }

        private string CopyTemporaryFiles(ISaveRequestHandler handler,
            UploadedFile[] oldFileList, UploadedFile[] newFileList, FilesToDelete filesToDelete)
        {
            foreach (var file in newFileList)
            {
                var filename = file.Filename.Trim();
                if (oldFileList.Any(x => String.Compare(x.Filename.Trim(), filename, StringComparison.OrdinalIgnoreCase) == 0))
                    continue;

                if (!filename.ToLowerInvariant().StartsWith("temporary/"))
                    throw new InvalidOperationException("For security reasons, only temporary files can be used in uploads!");

                //ImageUploadBehavior.CheckUploadedImageAndCreateThumbs(attr, ref filename);

                var idField = (Field)(((IIdRow)handler.Row).IdField);
                var copyResult = uploadHelper.CopyTemporaryFile(filename, idField.AsObject(handler.Row), filesToDelete,
                    s => ProcessReplaceFields(s, this.replaceFields, handler));

                if (!attr.SubFolder.IsEmptyOrNull())
                    copyResult.DbFileName = copyResult.DbFileName.Substring(attr.SubFolder.Length + 1);

                file.Filename = copyResult.DbFileName;
            }

            return JSON.Stringify(newFileList);
        }

        public override void OnAfterSave(ISaveRequestHandler handler)
        {
            if (handler.IsUpdate)
                return;

            var field = (StringField)Target;
            if (!handler.Row.IsAssigned(field))
                return;

            var newFilesJSON = field[handler.Row] = field[handler.Row].TrimToNull();
            var newFileList = ParseAndValidate(newFilesJSON, "newFiles");

            if (newFileList.IsEmptyOrNull())
                return;

            var filesToDelete = handler.StateBag[this.GetType().FullName + "_" + Target.Name + "_FilesToDelete"] as FilesToDelete;
            var copyResult = CopyTemporaryFiles(handler, new UploadedFile[0], newFileList, filesToDelete);
            var idField = (Field)(((IIdRow)handler.Row).IdField);

            new SqlUpdate(handler.Row.Table)
                .Set(field, copyResult)
                .Where(idField == new ValueCriteria(idField.AsObject(handler.Row)))
                .Execute(handler.UnitOfWork.Connection);

            field[handler.Row] = copyResult;
        }
    }
}
