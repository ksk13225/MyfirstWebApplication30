using MyfirstWebApplication5.Config;
using MyfirstWebApplication5.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using ClosedXML.Excel;
using System.Text.RegularExpressions;

namespace MyfirstWebApplication5.Controllers
{
    public class StockController : Controller
    {
       
        //ここからスタート(ActionResultとviewの名前をそろえる)
        public ActionResult GetAll()
        {
            return View();
        }

        public JsonResult GetStockJson()
        {

            List<Stock> stocks = new List<Stock>();
            return Json(stocks, JsonRequestBehavior.AllowGet);

        }

       
        //検索ボタン押下後
        public ActionResult Search(string search,int ? page,string select_stock)
        {


            //searchderedirect
            if (search == null) search = ""; // nullが渡されたら""を入れる

            //erakaihi
            if (select_stock == null) select_stock = "1";

            

            ViewBag.kassei = 1;

            List<Stock> stocks = GetStocks("Stocks_Search", search, select_stock);

            if(page > 0)
            {
                page = page;
            }
            else
            {
                page = 1;
            } 

            int limit = 50;
            int start = (int)(page - 1) * limit;
            int totalCount = stocks.Count();
            ViewBag.totalCount = totalCount;
            ViewBag.pageCurrent = page;

            int numberPage = (totalCount / limit);
            ViewBag.numberPage = numberPage;

            ViewBag.select_stock = select_stock;

            ViewBag.search = search;




            var data = stocks.Skip(start).Take(limit);
            //var data = stocks.OrderByDescending(s => s.Stock_Id).Skip(start).Take(limit);


            return View("GetAll", data);
            
            //return RedirectToAction("GetAll", data);
        }

        [HttpPost]
        public JsonResult test(string td)
        {
            //return Json("true", JsonRequestBehavior.AllowGet);
            return Json(td, JsonRequestBehavior.AllowGet);
        }








        //方針を変えてajaxで取得する、ボタンを押下されたタイミングではSQLが発行されていない
        //ファイル出力
        //[HttpPost]
        //public ActionResult FileOutput(string ntblstock, string select_stock, Stock stockss)
        //{

            public ActionResult FileOutput(string td)
            {

            var strList = new List<string>();
            string[] arr1 = Regex.Split(td, "\r\n|\n");

            foreach (string hairetu in arr1)
            {

                strList.Add(hairetu.Trim());
                strList.Remove("");
                strList.Remove("Edit");
                strList.Remove("Delete");
            }


            //新規のExcelブックを作成する
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("信用銘柄");


           
            for (var i = 0; i < strList.Count; i++)
            {
                //0から39の範囲内で偶数の場合上奇数の場合下

                if (i % 2 == 0)
                {
                    worksheet.Cell(i + 1, 1).Value = strList[i].ToString();
                }
                
                else
                {
                    worksheet.Cell(i, 2).Value = strList[i].ToString();

                }
                
            }

            //ajax nobaai


            //using (MemoryStream stream = new MemoryStream())
            //{
            //    workbook.SaveAs(stream);
            //    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ComList_{DateTime.Now.ToString("yyyyMMdd")}.xlsx");
            //}



            workbook.SaveAs(@"C:\Users\ksk\Desktop\プログラミング\C#MVC\youtubestudy\MyfirstWebApplication30\test2.xlsx");

            return RedirectToAction("GetAll");
        }



        public List<Stock> GetStocks(string storeProcedure, string search,string select_stock)
        {
                List<Stock> stocks = new List<Stock>();

           
            using (SqlConnection con = new SqlConnection(StoreConnection.GetConnection()))
            {

                using (SqlCommand cmd = new SqlCommand(storeProcedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    //if (search != null || select_stock != null) 
                        if (search != null)
                        cmd.Parameters.AddWithValue("@Filter", search);
                        cmd.Parameters.AddWithValue("@FilterID", select_stock);


                   
                    if (con.State != System.Data.ConnectionState.Open)
                    {
                        con.Open();

                        SqlDataReader sdr = cmd.ExecuteReader();

                        DataTable dtStocks = new DataTable();

                        dtStocks.Load(sdr);

                        foreach (DataRow row in dtStocks.Rows)
                        {
                            stocks.Add(
                                new Stock
                                {
                                    Stock_Id = Convert.ToInt32(row["CODE"]),
                                    Stock_Name = row["CODENAME"].ToString(),
                                    
                                }

                            );
                        }
                    }

                

                }

                ViewBag.stocks = stocks;
                return stocks;
            
            }
        }

        //Createの初期表示
        public ActionResult Create()
        {
            //新規登録時はCODEを表示させる
            ViewBag.hyouji = 1;
            return View();
        }

        //Createのsubmit押下後
        [HttpPost]
        public ActionResult Create(Stock stock)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection con = new SqlConnection(StoreConnection.GetConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand("Stock_Save_Update", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id", stock.Stock_Id);
                        cmd.Parameters.AddWithValue("@Name", stock.Stock_Name);
                        


                        if (con.State != System.Data.ConnectionState.Open)
                        {
                            con.Open();
                            cmd.ExecuteNonQuery();
                        }

                    }
                    //return RedirectToAction("GetAll");
                    return RedirectToAction("Search");
                }

            }
            //return Content("record save in the database");
            return View("Create", stock);
        }

        //ファイル出力ボタン




        //削除
        public ActionResult Delete(int id)
        {
            if (id < 0)
                return HttpNotFound();

            using (SqlConnection con = new SqlConnection(StoreConnection.GetConnection()))
            {
                using (SqlCommand cmd = new SqlCommand("Stock_DeleteById", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Id", id);

                    if (con.State != ConnectionState.Open)
                        con.Open();

                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("GetAll");
        }

        //編集
        public ActionResult Edit(int Id)
        {
            if (Id <= 0)
                return HttpNotFound();

            var _stock = new Stock();

            using (SqlConnection con = new SqlConnection(StoreConnection.GetConnection()))
            {

                using (SqlCommand cmd = new SqlCommand("Stock_GetById", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Id", Id);

                    if (con.State != ConnectionState.Open)
                        con.Open();
                    SqlDataReader sdr = cmd.ExecuteReader();
                    DataTable dt = new DataTable();

                    if (sdr.HasRows)
                    {
                        dt.Load(sdr);

                        DataRow row = dt.Rows[0];
                        _stock.Stock_Id = Convert.ToInt32(row["CODE"]);
                        _stock.Stock_Name = row["CODENAME"].ToString();
                        

                        return View("Create", _stock);

                    }
                    else
                    {
                        return HttpNotFound();
                    }

                }

            }
        }

    }
}