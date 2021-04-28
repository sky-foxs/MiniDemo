using MiniDemo.Data;
using MiniDemo.Helpers;
using MiniDemo.Tenant;
using MiniDemo.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Audit
{
    public class AuditPropertySetter : IAuditPropertySetter
    {
        protected ICurrentUser CurrentUser { get; }
        protected ICurrentTenant CurrentTenant { get; }
        public AuditPropertySetter(
         ICurrentUser currentUser,
         ICurrentTenant currentTenant)
        {
            CurrentUser = currentUser;
            CurrentTenant = currentTenant;
        }
        public void SetCreationProperties(object targetObject)
        {
            SetCreationTime(targetObject);
            SetCreatorId(targetObject);
        }

        public void SetModificationProperties(object targetObject)
        {
            SetLastModificationTime(targetObject);
            SetLastModifierId(targetObject);
        }

        public void SetDeletionProperties(object targetObject)
        {
            SetDeletionTime(targetObject);
            SetDeleterId(targetObject);
        }

        private void SetCreationTime(object targetObject)
        {
            if (!(targetObject is IHasCreationTime objectWithCreationTime))
            {
                return;
            }

            if (objectWithCreationTime.CreationTime == default)
            {
                ObjectHelper.TrySetProperty(objectWithCreationTime, x => x.CreationTime, () => DateTime.Now);
            }
        }

        private void SetCreatorId(object targetObject)
        {
            if (!CurrentUser.Id.HasValue)
            {
                return;
            }

            if (targetObject is IMultiTenant multiTenantEntity)
            {
                if (multiTenantEntity.TenantId != CurrentUser.TenantId)
                {
                    return;
                }
            }


            if (targetObject is IMayHaveCreator mayHaveCreatorObject)
            {
                if (mayHaveCreatorObject.CreatorId.HasValue && mayHaveCreatorObject.CreatorId.Value != default)
                {
                    return;
                }

                ObjectHelper.TrySetProperty(mayHaveCreatorObject, x => x.CreatorId, () => CurrentUser.Id);
            }
        }
        private void SetLastModificationTime(object targetObject)
        {
            if (targetObject is IHasModificationTime objectWithModificationTime)
            {
                objectWithModificationTime.LastModificationTime = DateTime.Now;
            }
        }

        private void SetLastModifierId(object targetObject)
        {
            if (!(targetObject is IModificationAuditedObject modificationAuditedObject))
            {
                return;
            }

            if (!CurrentUser.Id.HasValue)
            {
                modificationAuditedObject.LastModifierId = null;
                return;
            }

            if (modificationAuditedObject is IMultiTenant multiTenantEntity)
            {
                if (multiTenantEntity.TenantId != CurrentUser.TenantId)
                {
                    modificationAuditedObject.LastModifierId = null;
                    return;
                }
            }

            modificationAuditedObject.LastModifierId = CurrentUser.Id;
        }

        private void SetDeletionTime(object targetObject)
        {
            if (targetObject is IHasDeletionTime objectWithDeletionTime)
            {
                if (objectWithDeletionTime.DeletionTime == null)
                {
                    objectWithDeletionTime.DeletionTime = DateTime.Now;
                }
            }
        }

        private void SetDeleterId(object targetObject)
        {
            if (!(targetObject is IDeletionAuditedObject deletionAuditedObject))
            {
                return;
            }

            if (deletionAuditedObject.DeleterId != null)
            {
                return;
            }

            if (!CurrentUser.Id.HasValue)
            {
                deletionAuditedObject.DeleterId = null;
                return;
            }

            if (deletionAuditedObject is IMultiTenant multiTenantEntity)
            {
                if (multiTenantEntity.TenantId != CurrentUser.TenantId)
                {
                    deletionAuditedObject.DeleterId = null;
                    return;
                }
            }

            deletionAuditedObject.DeleterId = CurrentUser.Id;
        }

    }
}
