using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;
using Teigha.GraphicsInterface;
using Teigha.GraphicsSystem;

namespace EM.CAD
{
    /// <summary>
    /// 选择器
    /// </summary>
    public class Selector : SelectionReactor
    {
        ObjectIdCollection _objectIdCollection;
        ObjectId _spaceId;
        public Selector(ObjectIdCollection objectIdCollection, ObjectId spaceId)
        {
            _spaceId = spaceId;
            _objectIdCollection = objectIdCollection;
        }
        public override bool Selected(DrawableDesc pDrawableDesc)
        {
            DrawableDesc pDesc = pDrawableDesc;
            if (pDesc.Parent != null)
            {
                // we walk up the GS node path to the root container primitive
                // to avoid e.g. selection of individual lines in a dimension 
                while (((DrawableDesc)pDesc.Parent).Parent != null)
                    pDesc = (DrawableDesc)pDesc.Parent;
                if (pDesc.PersistId != IntPtr.Zero && ((DrawableDesc)pDesc.Parent).PersistId == _spaceId.OldIdPtr)
                {
                    pDesc.MarkedToSkip = true; // regen abort for selected drawable, to avoid duplicates
                    bool containItem = false;
                    foreach (ObjectId objectId in _objectIdCollection)
                    {
                        if (objectId.OldIdPtr == pDesc.PersistId)
                        {
                            containItem = true;
                            break;
                        }
                    }
                    if (!containItem)
                    {
                        _objectIdCollection.Add(new ObjectId(pDesc.PersistId));
                    }
                }
                return true;
            }
            return false;
        }
        // this more informative callback may be used to implement subentities selection
        public override SelectionReactorResult Selected(PathNode pthNode, Teigha.GraphicsInterface.Viewport viewInfo)
        {
            return SelectionReactorResult.NotImplemented;
        }

    }
}
