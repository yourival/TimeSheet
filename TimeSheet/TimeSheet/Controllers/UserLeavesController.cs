using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Web.Mvc;
using TimeSheet.Models;
using System.IO;
using System.Diagnostics;

namespace TimeSheet.Controllers
{
    public class UserLeavesController : Controller
    {
        private TimeSheetDb timesheetDb = new TimeSheetDb();
        
        [AuthorizeUser(Roles = "Manager")]
        // GET: UserLeaves/Index
        public ActionResult Index()
        {
            List<string> model = timesheetDb.LeaveBalances.Select(l => l.UserID).Distinct().ToList();

            return View(model);
        }

        // GET: UserLeaves/DisplayRow
        public ActionResult DisplayRow(string userId, int rowId)
        {
            return PartialView("_DisplayRow", GetBalanceVM(userId, rowId));
        }

        // GET: UserLeaves/EditRow
        public ActionResult EditRow(string userId, int rowId)
        {
            return PartialView("_EditRow", GetBalanceVM(userId, rowId));
        }

        public ActionResult ImportUserLeaves()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Preview()
        {
            HttpFileCollectionBase files = Request.Files;
            List<LeaveBalanceViewModel> model = new List<LeaveBalanceViewModel>();
            if (files.Count != 0)
                model = ReadCSV(files[0]);

            return PartialView("_Preview", model);
        }

        [HttpPost]
        public ActionResult ImportUserLeaves(HttpPostedFileBase file, string button)
        {
            if(button == "Save")
            {
                List<LeaveBalanceViewModel> model = new List<LeaveBalanceViewModel>();
                if (file != null)
                    model = ReadCSV(file);

                foreach(var balanceVM in model)
                {
                    for(int i=0; i<3; i++)
                    {
                        if (balanceVM.UserId != string.Empty)
                        {
                            LeaveBalance balance = timesheetDb.LeaveBalances.Find(balanceVM.UserId, (_leaveType)i);
                            if (balance == null)
                            {
                                balance = new LeaveBalance
                                {
                                    UserID = balanceVM.UserId,
                                    UserName = balanceVM.UserName,
                                    LeaveType = (_leaveType)i,
                                    AvailableLeaveHours = balanceVM.Balances[i]
                                };
                                timesheetDb.LeaveBalances.Add(balance);
                            }
                            else
                            {
                                balance.AvailableLeaveHours = balanceVM.Balances[i];
                                timesheetDb.Entry(balance).State = EntityState.Modified;
                            }
                        }
                    }
                    timesheetDb.SaveChanges();
                }
            }
            return RedirectToAction("Index");            
        }

        // POST: UserLeaves/EditRows
        [HttpPost]
        public ActionResult EditRow(string userId, int rowId, double[] balances)
        {
            for (int i = 0; i < 3; i++)
            {
                var leaveBalance = timesheetDb.LeaveBalances.Find(userId, (_leaveType)i);
                if (leaveBalance == null)
                {
                    timesheetDb.LeaveBalances.Add(new LeaveBalance()
                    {
                        UserID = userId,
                        LeaveType = (_leaveType)i,
                        AvailableLeaveHours = balances[i]
                    });
                }
                else
                {
                    leaveBalance.AvailableLeaveHours = balances[i];
                    timesheetDb.Entry(leaveBalance).State = EntityState.Modified;
                }
                timesheetDb.SaveChanges();
            }

            return PartialView("_DisplayRow", GetBalanceVM(userId, rowId));
        }

        private LeaveBalanceViewModel GetBalanceVM(string userId, int rowId)
        {
            double[] balances = new double[3];
            ADUser user = timesheetDb.ADUsers.Where(u => u.Email == userId).FirstOrDefault();
            string userName = (user == null) ? string.Empty : user.UserName;

            for (int i = 0; i < 3; i++)
            {
                var leaveBalance = timesheetDb.LeaveBalances.Find(userId, (_leaveType)i);
                balances[i] = (leaveBalance == null) ? 0 : leaveBalance.AvailableLeaveHours;
            }

            LeaveBalanceViewModel model = new LeaveBalanceViewModel()
            {
                RowId = rowId,
                UserId = userId,
                UserName = userName,
                Balances = balances
            };

            return model;
        }

        private List<LeaveBalanceViewModel> ReadCSV(HttpPostedFileBase file)
        {
            List<LeaveBalanceViewModel> model = new List<LeaveBalanceViewModel>();
            using (StreamReader csvreader = new StreamReader(file.InputStream))
            {
                LeaveBalanceViewModel balanceVM = new LeaveBalanceViewModel();
                int i = 1;
                while (!csvreader.EndOfStream)
                {
                    var row = csvreader.ReadLine();
                    var cols = row.Split(',');
                    if (cols.Length == 3)
                    {
                        balanceVM = new LeaveBalanceViewModel();
                        string lastname = cols[0].Replace("\"", string.Empty)
                                                 .Replace(" ", string.Empty);
                        string firstname = cols[1].Replace("\"", string.Empty)
                                                  .Replace(" ", string.Empty);
                        balanceVM.UserName = firstname + " " + lastname;
                    }
                    else if (cols.Length == 2)
                    {
                        double leaveHours = 0;

                        if (cols[0] != string.Empty && cols[1] == string.Empty)
                        {
                            balanceVM = new LeaveBalanceViewModel();
                            balanceVM.UserName = cols[0];
                        }
                        else if (cols[1] == string.Empty && cols[1] == string.Empty)
                        {
                            if (balanceVM != null)
                            {
                                ADUser user = timesheetDb.ADUsers
                                                       .Where(u => u.UserName == balanceVM.UserName)
                                                       .FirstOrDefault();
                                if (user == null)
                                {
                                    ModelState.AddModelError("", "User '" + balanceVM.UserName + "' doesn't exit in Database.");
                                }
                                else
                                {
                                    balanceVM.UserId = user.Email;
                                    balanceVM.UserName = balanceVM.UserName;
                                    model.Add(balanceVM);
                                }
                                balanceVM = null;
                            }
                        }
                        else if (Double.TryParse(cols[1], out leaveHours) || cols[1] == "0.00_x000D_")
                        {
                            switch (cols[0])
                            {
                                case "Holiday Leave Accrual":
                                    balanceVM.Balances[(int)_leaveType.annual] = leaveHours;
                                    break;
                                case "Sick Leave Accrual":
                                    balanceVM.Balances[(int)_leaveType.sick] = leaveHours;
                                    break;
                                case "Time in Lieu (Flex) Accrual":
                                    balanceVM.Balances[(int)_leaveType.flexi] = leaveHours;
                                    break;
                                default:
                                    ModelState.AddModelError("", "Undefined leaves type in row " + i + " .");
                                    break;
                            }
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Undefined format in row " + i + " .");
                    }
                    i++;
                }

                if (balanceVM != null)
                {
                    ADUser user = timesheetDb.ADUsers
                                            .Where(u => u.UserName == balanceVM.UserName)
                                            .FirstOrDefault();
                    if (user == null)
                    {
                        ModelState.AddModelError("", "User '" + balanceVM.UserName + "' doesn't exit in Database.");
                    }
                    else
                    {
                        balanceVM.UserId = user.Email;
                        balanceVM.UserName = balanceVM.UserName;
                        model.Add(balanceVM);
                        balanceVM = null;
                    }
                }  
            }
            return model;
        }
    }
}