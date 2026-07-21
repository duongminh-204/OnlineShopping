using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.Models.Admin.Accounts
{
    public interface AccountListenerInterface
    {
        public IActionResult CreateAccountSuccess();
        public IActionResult CreateAccountFails(ApplicationUser user);
        public IActionResult UpdateAccountSuccess();
        public IActionResult UpdateAccountFails(ApplicationUser user);
        public IActionResult ResetPasswordSuccess();
        public IActionResult ResetPasswordFails(ApplicationUser user, string message = null);
        public IActionResult BannedAccountSuccess();
        public IActionResult BannedAccountFails();
        public IActionResult DeleteAccountSuccess();
        public IActionResult DeleteAccountFails();
        public IActionResult PageNotFound();
    }
}
