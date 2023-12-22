using System;

namespace Studiofy.IDE.Models
{
    public class EditAction
    {
        public EditActionType EditActionType { get; set; }
        public string TextState { get; set; }
        public string TextInvolved { get; set; }
        public Range Selection { get; set; }
    }
}
