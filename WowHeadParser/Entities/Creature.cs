﻿/*
 * * Created by ArcaniaFr
 */
using Newtonsoft.Json;
using Sql;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using static WowHeadParser.MainWindow;

namespace WowHeadParser.Entities
{
    enum reactOrder
    {
        ALLIANCE    = 0,
        HORDE       = 1
    }

    class CreatureTemplateParsing
    {
        public int id;
        public int classification;
        public int family;
        public int type;
        public bool boss;
        public int hasQuests;
        public int minlevel;
        public int maxlevel;
        public int[] location;
        public string name;
        public string tag; // subname
        public string[] react;
        public String health;

        public String minGold;
        public String maxGold;
        public float healthModifier;
    }

    class NpcVendorParsing
    {
        public int classs;
        public int flags2;
        public int id;
        public int level;
        public string name;
        public int slot;
        public int[] source;
        public int subclass;
        public int standing;
        public int avail;
        public int[] stack;
        public dynamic cost;

        public int integerCost;
        public int integerExtendedCost;
        public int incrTime;
    }

    class CreatureLootParsing
    {
        public int id;
        public dynamic modes;
        public int[] stack;

        public string percent;
        public string questRequired;
        public string mode;
    }

    class CreatureLootItemParsing : CreatureLootParsing
    {
        public int classs;
        public int count;
        public int[] bonustrees;
    }

    class CreatureLootCurrencyParsing : CreatureLootParsing
    {
        public int category;
        public string icon;
    }

    class CreatureTrainerParsing
    {
        public int id;
        public int trainingcost;
        public int[] skill;
        public int learnedat;
        public int level;
    }

    class QuestStarterEnderParsing
    {
        public int category;
        public int category2;
        public int id;
        public int level;
        public int money;
        public string nam;
        public int reqlevel;
        public int side;
        public int xp;
    }

    class Creature : Entity
    {
        public Creature()
        {
            m_creatureTemplateData = new CreatureTemplateParsing();
            m_creatureTemplateData.id = 0;
        }

        public Creature(int id)
        {
            m_creatureTemplateData = new CreatureTemplateParsing();
            m_creatureTemplateData.id = id;
        }

        public override String GetWowheadUrl()
        {
            return GetWowheadBaseUrl() + "/npc=" + m_creatureTemplateData.id;
        }

        public override List<Entity> GetIdsFromZone(String zoneId, String zoneHtml)
        {
            String pattern = @"new Listview\(\{template: 'npc', id: 'npcs', name: LANG\.tab_npcs, tabs: tabsRelated, parent: 'lkljbjkb574', note: WH\.sprintf\(LANG\.lvnote_filterresults, '\/npcs\?filter=cr=6;crs=" + zoneId + @";crv=0'\), data: (.+)\}\);";
            String creatureJSon = Tools.ExtractJsonFromWithPattern(zoneHtml, pattern);

            List<CreatureTemplateParsing> parsingArray = JsonConvert.DeserializeObject<List<CreatureTemplateParsing>>(creatureJSon);
            List<Entity> tempArray = new List<Entity>();
            foreach (CreatureTemplateParsing creatureTemplateStruct in parsingArray)
            {
                Creature creature = new Creature(creatureTemplateStruct.id);
                tempArray.Add(creature);
            }

            return tempArray;
        }

        public override bool ParseSingleJson(int id = 0)
        {
            if (m_creatureTemplateData.id == 0 && id == 0)
                return false;
            else if (m_creatureTemplateData.id == 0 && id != 0)
                m_creatureTemplateData.id = id;

            String creatureHtml = Tools.GetHtmlFromWowhead(GetWowheadUrl(), webClient);

            if (creatureHtml.Contains("inputbox-error") || creatureHtml.Contains("database-detail-page-not-found-message"))
            {
                return false;
            }
            else
            { 
                String dataPattern = @"\$\.extend\(g_npcs\[" + m_creatureTemplateData.id + @"\], (.+)\);";
                String creatureHealthPattern = @"<div>(?:Health|Vie) : ((?:\d|,|\.)+)</div>";
                String creatureMoneyPattern = @"\[money=([0-9]+)\]";
                String creatureModelIdPattern = @"WH\.ModelViewer\.showLightbox\({&quot;type&quot;:[0-9]+,&quot;typeId&quot;:" + m_creatureTemplateData.id + @",&quot;displayId&quot;:([0-9]+)}\)";
                String creatureTemplateDataJSon = Tools.ExtractJsonFromWithPattern(creatureHtml, dataPattern);
                if (creatureTemplateDataJSon != null)
                {
                    CreatureTemplateParsing creatureTemplateData = JsonConvert.DeserializeObject<CreatureTemplateParsing>(creatureTemplateDataJSon);

                    String creatureHealthDataJSon = Tools.ExtractJsonFromWithPattern(creatureHtml, creatureHealthPattern);
                    String creatureMoneyData = Tools.ExtractJsonFromWithPattern(creatureHtml, creatureMoneyPattern);
                    String creatureModelIdData = Tools.ExtractJsonFromWithPattern(creatureHtml, creatureModelIdPattern);
                    SetCreatureTemplateData(creatureTemplateData, creatureModelIdData, creatureMoneyData, creatureHealthDataJSon);

                    // Without m_creatureTemplateData we can't really do anything, so return false
                    if (m_creatureTemplateData == null)
                        return false;
                }

                if (IsCheckboxChecked("vendor"))
                {
                    String vendorPattern = @"new Listview\({template: 'item', id: 'sells', .+?, data: (.+)}\);";
                    String npcVendorJSon = Tools.ExtractJsonFromWithPattern(creatureHtml, vendorPattern);
                    if (npcVendorJSon != null)
                    {
                        NpcVendorParsing[] npcVendorDatas = JsonConvert.DeserializeObject<NpcVendorParsing[]>(npcVendorJSon);
                        SetNpcVendorData(npcVendorDatas);
                    }
                }

                if (IsCheckboxChecked("loot"))
                {
                    String creatureLootPattern = @"new Listview\({template: 'item', id: 'drops', name: LANG\.tab_drops, tabs: tabsRelated, parent: 'lkljbjkb574', extraCols: \['count', 'percent'\],  showLootSpecs: [0-9],sort:\['noteworthy', '-percent', 'name'\], _totalCount: [0-9]+, computeDataFunc: Listview\.funcBox\.initLootTable, onAfterCreate: Listview\.funcBox\.addModeIndicator, data:(.+)}\);";
                    String creatureCurrencyPattern = @"new Listview\({template: 'currency', id: 'drop-currency', name: LANG\.tab_currencies, tabs: tabsRelated, parent: 'lkljbjkb574', extraCols: \[Listview\.extraCols\.count, Listview\.extraCols\.percent\], sort:\['-percent', 'name'], _totalCount: [0-9]*, computeDataFunc: Listview\.funcBox\.initLootTable, onAfterCreate: Listview\.funcBox\.addModeIndicator, data: (.+)}\);";

                    String creatureLootJSon = Tools.ExtractJsonFromWithPattern(creatureHtml, creatureLootPattern);
                    String creatureLootCurrencyJSon = Tools.ExtractJsonFromWithPattern(creatureHtml, creatureCurrencyPattern);
                    if (creatureLootJSon != null || creatureLootCurrencyJSon != null)
                    {
                        CreatureLootItemParsing[] creatureLootDatas = creatureLootJSon != null ? JsonConvert.DeserializeObject<CreatureLootItemParsing[]>(creatureLootJSon) : new CreatureLootItemParsing[0];
                        CreatureLootCurrencyParsing[] creatureLootCurrencyDatas = creatureLootCurrencyJSon != null ? JsonConvert.DeserializeObject<CreatureLootCurrencyParsing[]>(creatureLootCurrencyJSon) : new CreatureLootCurrencyParsing[0];

                        SetCreatureLootData(creatureLootDatas, creatureLootCurrencyDatas);
                    }
                }

                if (IsCheckboxChecked("skinning"))
                {
                    String creatureSkinningPattern = @"new Listview\(\{template: 'item', id: 'skinning', name: LANG\.tab_skinning, tabs: tabsRelated, parent: 'lkljbjkb574', extraCols: \[Listview\.extraCols\.count, Listview\.extraCols\.percent\], sort:\['-percent', 'name'\], computeDataFunc: Listview\.funcBox\.initLootTable, note: WH\.sprintf\(LANG\.lvnote_npcskinning, [0-9]+\), _totalCount: ([0-9]+), data: (.+)}\);";

                    String creatureSkinningCount = Tools.ExtractJsonFromWithPattern(creatureHtml, creatureSkinningPattern, 0);
                    String creatureSkinningJSon = Tools.ExtractJsonFromWithPattern(creatureHtml, creatureSkinningPattern, 1);
                    if (creatureSkinningJSon != null)
                    {
                        CreatureLootItemParsing[] creatureLootDatas = JsonConvert.DeserializeObject<CreatureLootItemParsing[]>(creatureSkinningJSon);
                        SetCreatureSkinningData(creatureLootDatas, Int32.Parse(creatureSkinningCount));
                    }
                }

                if (IsCheckboxChecked("pickpocketing"))
                {
                
                    String creaturePickpocketingPattern = @"new Listview\(\{template: 'item', id: 'pickpocketing', name: LANG\.tab_pickpocketing, tabs: tabsRelated, parent: 'lkljbjkb574', extraCols: \[Listview\.extraCols\.count, Listview\.extraCols\.percent\], sort:\['-percent', 'name'\], computeDataFunc: Listview\.funcBox\.initLootTable, note: WH\.sprintf\(LANG\.lvnote_npcpickpocketing, [0-9]+\), _totalCount: ([0-9]+), data:(.+)}\);";

                    String creaturePickpocketingCount = Tools.ExtractJsonFromWithPattern(creatureHtml, creaturePickpocketingPattern, 0);
                    String creaturePickpocketingJSon = Tools.ExtractJsonFromWithPattern(creatureHtml, creaturePickpocketingPattern, 1);
                    if (creaturePickpocketingJSon != null)
                    {
                        CreatureLootItemParsing[] creatureLootDatas = JsonConvert.DeserializeObject<CreatureLootItemParsing[]>(creaturePickpocketingJSon);
                        SetCreaturePickpocketingData(creatureLootDatas, Int32.Parse(creaturePickpocketingCount));
                    }
                }

                if (IsCheckboxChecked("trainer"))
                {
                    String creatureTrainerPattern = @"new Listview\(\{template: 'spell', id: 'teaches-recipe', name: LANG\.tab_teaches, tabs: tabsRelated, parent: 'lkljbjkb574', visibleCols: \['source'\], data: (.+)\}\);";

                    String creatureTrainerJSon = Tools.ExtractJsonFromWithPattern(creatureHtml, creatureTrainerPattern);
                    if (creatureTrainerJSon != null)
                    {
                        CreatureTrainerParsing[] creatureTrainerDatas = JsonConvert.DeserializeObject<CreatureTrainerParsing[]>(creatureTrainerJSon);
                        m_creatureTrainerDatas = creatureTrainerDatas;
                    }
                }

                if (IsCheckboxChecked("quest starter"))
                {
                    String creatureQuestStarterPattern = @"new Listview\(\{template: 'quest', id: 'starts', name: LANG\.tab_starts, tabs: tabsRelated, parent: 'lkljbjkb574', data: (.+)\}\);";

                    String creatureQuestStarterJSon = Tools.ExtractJsonFromWithPattern(creatureHtml, creatureQuestStarterPattern);
                    if (creatureQuestStarterJSon != null)
                    {
                        QuestStarterEnderParsing[] creatureQuestStarterDatas = JsonConvert.DeserializeObject<QuestStarterEnderParsing[]>(creatureQuestStarterJSon);
                        m_creatureQuestStarterDatas = creatureQuestStarterDatas;
                    }
                }

                if (IsCheckboxChecked("quest ender"))
                {
                    String creatureQuestEnderPattern = @"new Listview\(\{template: 'quest', id: 'ends', name: LANG\.tab_ends, tabs: tabsRelated, parent: 'lkljbjkb574', data: (.+)\}\);";

                    String creatureQuestEnderJSon = Tools.ExtractJsonFromWithPattern(creatureHtml, creatureQuestEnderPattern);
                    if (creatureQuestEnderJSon != null)
                    {
                        QuestStarterEnderParsing[] creatureQuestEnderDatas = JsonConvert.DeserializeObject<QuestStarterEnderParsing[]>(creatureQuestEnderJSon);
                        m_creatureQuestEnderDatas = creatureQuestEnderDatas;
                    }
                }

                return true;
            }
        }

        public void SetCreatureTemplateData(CreatureTemplateParsing creatureData, String modelid, String money, String creatureHealthDataJSon)
        {
            m_creatureTemplateData = creatureData;

            m_isBoss = false;
            m_faction = GetFactionFromReact();
            m_modelid = int.Parse(modelid);

            if (m_creatureTemplateData.minlevel == 9999 || m_creatureTemplateData.maxlevel == 9999)
            {
                m_isBoss = true;
                m_creatureTemplateData.minlevel = 100;
                m_creatureTemplateData.maxlevel = 100;
            }

            m_subname = m_creatureTemplateData.tag ?? "";

            m_creatureTemplateData.minGold = "0";
            m_creatureTemplateData.maxGold = "0";

            decimal averageMoney = 0;
            if (Decimal.TryParse(money, out averageMoney))
            {
                int roundNumber = Math.Min((int)Math.Pow(10.0, (double)(money.Length - 1)), 10000);

                m_creatureTemplateData.minGold = (((int)Math.Floor(averageMoney / roundNumber)) * roundNumber).ToString();
                m_creatureTemplateData.maxGold = (((int)Math.Ceiling(averageMoney / roundNumber)) * roundNumber).ToString();
            }
            
            if (creatureHealthDataJSon != null)
                m_creatureTemplateData.health = creatureHealthDataJSon.Replace(",", "");
        }

        public void SetNpcVendorData(NpcVendorParsing[] npcVendorDatas)
        {
            for (uint i = 0; i < npcVendorDatas.Length; ++i)
            {
                npcVendorDatas[i].avail = npcVendorDatas[i].avail == -1 ? 0 : npcVendorDatas[i].avail;
                npcVendorDatas[i].incrTime = npcVendorDatas[i].avail != 0 ? 3600 : 0;

                try
                {
                    int cost = Convert.ToInt32(npcVendorDatas[i].cost[0]);
                    npcVendorDatas[i].integerCost = cost;

                    List<Int32> itemId          = new List<Int32>();
                    List<Int32> itemCount       = new List<Int32>();

                    List<Int32> currencyId      = new List<Int32>();
                    List<Int32> currencyCount   = new List<Int32>();

                    foreach (JArray itemCost in npcVendorDatas[i].cost[2])
                    {
                        itemId.Add(Convert.ToInt32(itemCost[0]));
                        itemCount.Add(Convert.ToInt32(itemCost[1]));
                    }

                    foreach (JArray currencyCost in npcVendorDatas[i].cost[1])
                    {
                        currencyId.Add(Convert.ToInt32(currencyCost[0]));
                        currencyCount.Add(Convert.ToInt32(currencyCost[1]));
                    }

                    npcVendorDatas[i].integerExtendedCost = (int)Tools.GetExtendedCostId(itemId, itemCount, currencyId, currencyCount);
                }
                catch (Exception ex)
                {
                    npcVendorDatas[i].integerCost = 0;
                    npcVendorDatas[i].integerExtendedCost = 0;
                    Console.WriteLine("Erreur : " + ex);
                }
            }

            m_npcVendorDatas = npcVendorDatas;
        }

        public void SetCreatureLootData(CreatureLootItemParsing[] creatureLootItemDatas, CreatureLootCurrencyParsing[] creatureLootCurrencyDatas)
        {
            List<CreatureLootParsing> lootsData = new List<CreatureLootParsing>();

            List<String> modes = new List<String>() { "4", "8", "16", "32", "64", "65536", "131072", "33554432" };

            CreatureLootParsing[] allLootData = new CreatureLootParsing[creatureLootItemDatas.Length + creatureLootCurrencyDatas.Length];
            Array.Copy(creatureLootItemDatas, allLootData, creatureLootItemDatas.Length);
            Array.Copy(creatureLootCurrencyDatas, 0, allLootData, creatureLootItemDatas.Length, creatureLootCurrencyDatas.Length);

            for (uint i = 0; i < allLootData.Length; ++i)
            {
                float count = 0.0f;
                float outof = 0.0f;
                float percent = 0.0f;
                String currentMode = "";

                try
                {
                    String realItemMode = allLootData[i].modes["mode"];
                    String treatmentItemMode = realItemMode;

                    switch (realItemMode)
                    {
                        case "24":
                            treatmentItemMode = "8";
                            break;
                        case "33554433":
                        case "33554434":
                            treatmentItemMode = "4";
                            break;
                    }

                    count = (float)Convert.ToDouble(allLootData[i].modes[treatmentItemMode]["count"]);
                    outof = (float)Convert.ToDouble(allLootData[i].modes[treatmentItemMode]["outof"]);

                    if (count == -1 || count > outof)
                        percent = 0;
                    else
                        percent = count * 100 / outof;

                    if (count < 25 && percent < 0.05f)
                        continue;

                    currentMode = realItemMode;
                }
                catch (Exception)
                {
                    foreach (String mode in modes)
                    {
                        try
                        {
                            count = (float)Convert.ToDouble(allLootData[i].modes[mode]["count"]);
                            outof = (float)Convert.ToDouble(allLootData[i].modes[mode]["outof"]);
                            percent = count * 100 / outof;

                            if (count < 25 && percent < 0.05f)
                                continue;

                            currentMode = mode;
                            break;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                if (currentMode == "")
                {
                    continue;
                }

                CreatureLootItemParsing currentItemParsing = null;
                try
                {
                    currentItemParsing = (CreatureLootItemParsing)allLootData[i];
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur : " + ex);
                }

                allLootData[i].questRequired = (currentItemParsing != null && currentItemParsing.classs == 12) ? "1" : "0";
                allLootData[i].percent = Tools.NormalizeFloat(percent);
                allLootData[i].mode = currentMode;
                lootsData.Add(allLootData[i]);
            }

            m_creatureLootDatas = lootsData.ToArray();
        }
        
        public void SetCreatureSkinningData(CreatureLootItemParsing[] creatureSkinningDatas, int totalCount)
        {
            for (uint i = 0; i < creatureSkinningDatas.Length; ++i)
            {
                float percent = (float)creatureSkinningDatas[i].count * 100 / (float)totalCount;

                creatureSkinningDatas[i].percent = Tools.NormalizeFloat(percent);
            }

            m_creatureSkinningDatas = creatureSkinningDatas;
        }

        public void SetCreaturePickpocketingData(CreatureLootItemParsing[] creaturePickpocketingDatas, int totalCount)
        {
            for (uint i = 0; i < creaturePickpocketingDatas.Length; ++i)
            {
                float percent = (float)creaturePickpocketingDatas[i].count * 100 / (float)totalCount;

                creaturePickpocketingDatas[i].percent = Tools.NormalizeFloat(percent);
            }

            m_creaturePickpocketingDatas = creaturePickpocketingDatas;
        }

        private int GetFactionFromReact()
        {
            if (m_creatureTemplateData.react == null)
                return 14;

            if (m_creatureTemplateData.react[(int)reactOrder.ALLIANCE] == "1" && m_creatureTemplateData.react[(int)reactOrder.HORDE] == "1")
                return 35; // Ennemis
            else if (m_creatureTemplateData.react[(int)reactOrder.ALLIANCE] == "1" && m_creatureTemplateData.react[(int)reactOrder.HORDE] == "-1")
                return 11; // Hurlevent
            else if (m_creatureTemplateData.react[(int)reactOrder.ALLIANCE] == "-1" && m_creatureTemplateData.react[(int)reactOrder.HORDE] == "1")
                return 85; // Orgrimmar
            else if (m_creatureTemplateData.react[(int)reactOrder.ALLIANCE] == "0" && m_creatureTemplateData.react[(int)reactOrder.HORDE] == "0")
                return 2240; // Neutral

            return 14;
        }

        public override String GetSQLRequest()
        {
            String returnSql = "";

            if (m_creatureTemplateData.id == 0 || isError)
                return returnSql;

            // Creature Template
            if (IsCheckboxChecked("template"))
            {
                m_creatureTemplateBuilder = new SqlBuilder("creature_template", "entry");
                m_creatureTemplateBuilder.SetFieldsNames("name", "subname", "minlevel", "maxlevel", "faction", "family", "type");
                if(m_creatureTemplateData.minlevel != 0 && m_creatureTemplateData.maxlevel != 0) { 
                    m_creatureTemplateBuilder.AppendFieldsValue(m_creatureTemplateData.id, m_creatureTemplateData.name, m_subname ?? "", m_creatureTemplateData.minlevel, m_creatureTemplateData.maxlevel, m_faction, m_creatureTemplateData.family, m_creatureTemplateData.type);
                    returnSql += m_creatureTemplateBuilder.ToString() + "\n";
                }
            }

            if (IsCheckboxChecked("health modifier") && m_creatureTemplateData.health != null)
            {
                SqlBuilder builder = new SqlBuilder("creature_template", "entry", SqlQueryType.Update);
                builder.SetFieldsNames("HealthModifier");

                String healthModifier = Tools.GetHealthModifier(float.Parse(m_creatureTemplateData.health), 6, m_creatureTemplateData.minlevel, 1);

                builder.AppendFieldsValue(m_creatureTemplateData.id, healthModifier);
                returnSql += builder.ToString() + "\n";
            }

            // faction
            if (IsCheckboxChecked("simple faction"))
            {
                SqlBuilder m_creatureFactionBuilder = new SqlBuilder("creature_template", "entry", SqlQueryType.Update);
                m_creatureFactionBuilder.SetFieldsNames("faction");

                m_creatureFactionBuilder.AppendFieldsValue(m_creatureTemplateData.id, m_faction);
                returnSql += m_creatureFactionBuilder.ToString() + "\n";
            }

            // Creature template model
            if (IsCheckboxChecked("creature model"))
            {
                if(m_modelid != 0)
                {
                    SqlBuilder m_creatureModelInfoBuilder = new SqlBuilder("creature_model_info", "DisplayID", SqlQueryType.InsertOrUpdate);
                    SqlBuilder m_creatureTemplateModelBuilder = new SqlBuilder("creature_template_model", "CreatureID", SqlQueryType.InsertOrUpdate);
                    m_creatureModelInfoBuilder.SetFieldsNames("BoundingRadius", "CombatReach", "DisplayID_Other_Gender");
                    m_creatureTemplateModelBuilder.SetFieldsNames("Idx", "CreatureDisplayID", "DisplayScale", "Probability");

                    m_creatureModelInfoBuilder.AppendFieldsValue(m_modelid, "1", "1", "0");
                    m_creatureTemplateModelBuilder.AppendFieldsValue(m_creatureTemplateData.id, "0", m_modelid, "1", "1");
                    returnSql += m_creatureModelInfoBuilder.ToString() + "\n";
                    returnSql += m_creatureTemplateModelBuilder.ToString() + "\n";
                }
            }

            // Creature Template
            if (IsCheckboxChecked("money"))
            {
                SqlBuilder m_creatureMoneyBuilder = new SqlBuilder("creature_template", "entry", SqlQueryType.Update);
                m_creatureMoneyBuilder.SetFieldsNames("mingold", "maxgold");
                if(m_creatureTemplateData.minGold != null && m_creatureTemplateData.maxGold != null && m_creatureTemplateData.minGold != "0" && m_creatureTemplateData.maxGold != "0")
                {
                    m_creatureMoneyBuilder.AppendFieldsValue(m_creatureTemplateData.id, m_creatureTemplateData.minGold, m_creatureTemplateData.maxGold);
                    returnSql += m_creatureMoneyBuilder.ToString() + "\n";
                }
            }

            // Locales
            if (IsCheckboxChecked("locale"))
            {
                LocaleConstant localeIndex = (LocaleConstant)Properties.Settings.Default.localIndex;

                String localeName = localeIndex.ToString();

                if (localeIndex != 0)
                {
                    m_creatureLocalesBuilder = new SqlBuilder("creature_template_locales", "entry");
                    m_creatureLocalesBuilder.SetFieldsNames("locale", "Name", "Title");

                    m_creatureLocalesBuilder.AppendFieldsValue(m_creatureTemplateData.id, localeIndex.ToString(), m_creatureTemplateData.name, m_subname ?? "");
                    returnSql += m_creatureLocalesBuilder.ToString() + "\n";
                }
                else
                {
                    m_creatureLocalesBuilder = new SqlBuilder("creature_template", "entry");
                    m_creatureLocalesBuilder.SetFieldsNames("name", "subname");

                    m_creatureLocalesBuilder.AppendFieldsValue(m_creatureTemplateData.id, m_creatureTemplateData.name, m_subname ?? "");
                    returnSql += m_creatureLocalesBuilder.ToString() + "\n";
                }
            }

            if (IsCheckboxChecked("vendor") && m_npcVendorDatas != null)
            {
                m_npcVendorBuilder = new SqlBuilder("npc_vendor", "entry", SqlQueryType.DeleteInsert);
                m_npcVendorBuilder.SetFieldsNames("slot", "item", "maxcount", "incrtime", "ExtendedCost", "type", "PlayerConditionID");
                int Slot = 1;
                foreach (NpcVendorParsing npcVendorData in m_npcVendorDatas)
                {
                    m_npcVendorBuilder.AppendFieldsValue(m_creatureTemplateData.id, Slot, npcVendorData.id, npcVendorData.avail, npcVendorData.incrTime, npcVendorData.integerExtendedCost, 1, 0);
                    ++Slot;
                }

                returnSql += m_npcVendorBuilder.ToString() + "\n";
            }

            if (IsCheckboxChecked("loot") && m_creatureLootDatas != null)
            {
                bool referenceAdded = false;
                int maxReferenceLoot = 2; // A voir si on peut trouver

                int templateEntry = m_creatureTemplateData.id;
                m_creatureLootBuilder = new SqlBuilder("creature_loot_template", "entry", SqlQueryType.DeleteInsert);
                m_creatureLootBuilder.SetFieldsNames("Item", "Reference", "Chance", "QuestRequired", "LootMode", "GroupId", "MinCount", "MaxCount", "Comment");

                m_creatureReferenceLootBuilder = new SqlBuilder("reference_loot_template", "entry", SqlQueryType.DeleteInsert);
                m_creatureReferenceLootBuilder.SetFieldsNames("Item", "Reference", "Chance", "QuestRequired", "LootMode", "GroupId", "MinCount", "MaxCount", "Comment");

                returnSql += "UPDATE creature_template SET lootid = " + templateEntry + " WHERE entry = " + templateEntry + " AND lootid = 0;\n";
                foreach (CreatureLootParsing creatureLootData in m_creatureLootDatas)
                {
                    List<int> entryList = new List<int>();

                    CreatureLootItemParsing creatureLootItemData = null;
                    try
                    {
                        creatureLootItemData = (CreatureLootItemParsing)creatureLootData;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Erreur : " + ex);
                    }

                    CreatureLootCurrencyParsing creatureLootCurrencyData = null;
                    try
                    {
                        creatureLootCurrencyData = (CreatureLootCurrencyParsing)creatureLootData;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Erreur : " + ex);
                    }

                    int minLootCount = creatureLootData.stack.Length >= 1 ? creatureLootData.stack[0] : 1;
                    int maxLootCount = creatureLootData.stack.Length >= 2 ? creatureLootData.stack[1] : minLootCount;

                    // If bonuses, certainly an important loot, set to references
                    if (!IsCheckboxChecked("Is Dungeon/Raid Boss") || (creatureLootItemData == null || creatureLootItemData.bonustrees == null))
                    {
                        switch (creatureLootData.mode)
                        {
                            default:
                                entryList.Add(templateEntry);
                                break; ;
                        }

                        foreach (int entry in entryList)
                        {
                            int idMultiplier = creatureLootCurrencyData != null ? -1 : 1;

                            if (idMultiplier < 1)
                                continue;

                            m_creatureLootBuilder.AppendFieldsValue(entry, // Entry
                                                                    creatureLootData.id * idMultiplier, // Item
                                                                    0, // Reference
                                                                    creatureLootData.percent, // Chance
                                                                    creatureLootData.questRequired, // QuestRequired
                                                                    1, // LootMode
                                                                    0, // GroupId
                                                                    minLootCount, // MinCount
                                                                    maxLootCount, // MaxCount
                                                                    ""); // Comment
                        }
                    }
                    else
                    {
                        if (!referenceAdded)
                        {
                            m_creatureLootBuilder.AppendFieldsValue(templateEntry, // Entry
                                                                    0, // Item
                                                                    templateEntry, // Reference
                                                                    100, // Chance
                                                                    0, // QuestRequired
                                                                    1, // LootMode
                                                                    0, // GroupId
                                                                    maxReferenceLoot, // MinCount
                                                                    maxReferenceLoot, // MaxCount
                                                                    ""); // Comment
                            referenceAdded = true;
                        }

                        m_creatureReferenceLootBuilder.AppendFieldsValue(templateEntry, // Entry
                                                                         creatureLootData.id, // Item
                                                                         0, // Reference
                                                                         creatureLootData.percent, // Chance
                                                                         creatureLootData.questRequired, // QuestRequired
                                                                         1, // LootMode
                                                                         1, // GroupId
                                                                         minLootCount, // MinCount
                                                                         maxLootCount, // MaxCount
                                                                         ""); // Comment
                    }
                }

                returnSql += m_creatureLootBuilder.ToString() + "\n";
                returnSql += m_creatureReferenceLootBuilder.ToString() + "\n";
            }

            if (IsCheckboxChecked("skinning") && m_creatureSkinningDatas != null)
            {
                m_creatureSkinningBuilder = new SqlBuilder("skinning_loot_template", "entry", SqlQueryType.DeleteInsert);
                m_creatureSkinningBuilder.SetFieldsNames("Item", "Reference", "Chance", "QuestRequired", "LootMode", "GroupId", "MinCount", "MaxCount", "Comment");

                returnSql += "UPDATE creature_template SET skinloot = " + m_creatureTemplateData.id + " WHERE entry = " + m_creatureTemplateData.id + " AND skinloot = 0;\n";
                foreach (CreatureLootParsing creatureSkinningData in m_creatureSkinningDatas)
                {
                    m_creatureSkinningBuilder.AppendFieldsValue(m_creatureTemplateData.id, // Entry
                                                                creatureSkinningData.id, // Item
                                                                0, // Reference
                                                                creatureSkinningData.percent, // Chance
                                                                0, // QuestRequired
                                                                1, // LootMode
                                                                0, // GroupId
                                                                creatureSkinningData.stack[0], // MinCount
                                                                creatureSkinningData.stack[1], // MaxCount
                                                                ""); // Comment
                }

                returnSql += m_creatureSkinningBuilder.ToString() + "\n";
            }

            if (IsCheckboxChecked("pickpocketing") && m_creaturePickpocketingDatas != null)
            {
                m_creaturePickpocketingBuilder = new SqlBuilder("pickpocketing_loot_template", "entry", SqlQueryType.DeleteInsert);
                m_creaturePickpocketingBuilder.SetFieldsNames("Item", "Reference", "Chance", "QuestRequired", "LootMode", "GroupId", "MinCount", "MaxCount", "Comment");

                returnSql += "UPDATE creature_template SET pickpocketloot = " + m_creatureTemplateData.id + " WHERE entry = " + m_creatureTemplateData.id + " AND pickpocketloot = 0;\n";
                foreach (CreatureLootParsing creaturePickpocketingData in m_creaturePickpocketingDatas)
                {
                    m_creaturePickpocketingBuilder.AppendFieldsValue(m_creatureTemplateData.id, // Entry
                                                                creaturePickpocketingData.id, // Item
                                                                0, // Reference
                                                                creaturePickpocketingData.percent, // Chance
                                                                0, // QuestRequired
                                                                1, // LootMode
                                                                0, // GroupId
                                                                creaturePickpocketingData.stack[0], // MinCount
                                                                creaturePickpocketingData.stack[1], // MaxCount
                                                                ""); // Comment
                }

                returnSql += m_creaturePickpocketingBuilder.ToString() + "\n";
            }

            if (IsCheckboxChecked("trainer") && m_creatureTrainerDatas != null)
            {
                m_creatureTrainerBuilder = new SqlBuilder("npc_trainer", "entry", SqlQueryType.DeleteInsert);
                m_creatureTrainerBuilder.SetFieldsNames("spell", "spellcost", "reqskill", "reqskillvalue", "reqlevel");

                returnSql += "UPDATE creature_template SET npc_flag = 16 WHERE entry = " + m_creatureTemplateData.id + ";\n";
                foreach (CreatureTrainerParsing creatureTrainerData in m_creatureTrainerDatas)
                {
                    int reqskill = 0;
                    int Learnedat = 0;
                    int Level = 0;
                    if (creatureTrainerData.learnedat != 9999)
                    {
                        if (creatureTrainerData.learnedat == 1)
                        { 
                            Learnedat = 0;
                            reqskill = 0;
                        }
                        else
                        {
                            Learnedat = creatureTrainerData.learnedat;
                            reqskill = creatureTrainerData.learnedat > 0 ? creatureTrainerData.skill[0] : 0;
                        }

                        Level = creatureTrainerData.level;
                    }
                    m_creatureTrainerBuilder.AppendFieldsValue(m_creatureTemplateData.id, creatureTrainerData.id, creatureTrainerData.trainingcost, reqskill, Learnedat, Level);
                }

                returnSql += m_creatureTrainerBuilder.ToString() + "\n";
            }

            if (IsCheckboxChecked("quest starter") && m_creatureQuestStarterDatas != null)
            {
                m_creatureQuestStarterBuilder = new SqlBuilder("creature_queststarter", "id", SqlQueryType.DeleteInsert);
                m_creatureQuestStarterBuilder.SetFieldsNames("quest");

                foreach (QuestStarterEnderParsing creatureQuestStarterData in m_creatureQuestStarterDatas)
                    m_creatureQuestStarterBuilder.AppendFieldsValue(m_creatureTemplateData.id, creatureQuestStarterData.id);
                returnSql += "UPDATE `creature_template` SET `npcflag`='3' WHERE `entry`='" + m_creatureTemplateData.id + "' AND `npcflag`='0';\n";
                returnSql += m_creatureQuestStarterBuilder.ToString() + "\n";
            }

            if (IsCheckboxChecked("quest ender") && m_creatureQuestEnderDatas != null)
            {
                m_creatureQuestEnderBuilder = new SqlBuilder("creature_questender", "id", SqlQueryType.DeleteInsert);
                m_creatureQuestEnderBuilder.SetFieldsNames("quest");

                foreach (QuestStarterEnderParsing creatureQuestEnderData in m_creatureQuestEnderDatas)
                    m_creatureQuestEnderBuilder.AppendFieldsValue(m_creatureTemplateData.id, creatureQuestEnderData.id);

                returnSql += "UPDATE `creature_template` SET `npcflag`='3' WHERE `entry`='" + m_creatureTemplateData.id + "' AND `npcflag`='0';\n";
                returnSql += m_creatureQuestEnderBuilder.ToString() + "\n";
            }

            return returnSql;
        }

        private int m_faction;
        private bool m_isBoss;
        private int m_modelid;
        private String m_subname;

        protected CreatureTemplateParsing m_creatureTemplateData;
        protected NpcVendorParsing[] m_npcVendorDatas;
        protected CreatureLootParsing[] m_creatureLootDatas;
        protected CreatureLootItemParsing[] m_creatureSkinningDatas;
        protected CreatureLootItemParsing[] m_creaturePickpocketingDatas;
        protected CreatureTrainerParsing[] m_creatureTrainerDatas;
        protected QuestStarterEnderParsing[] m_creatureQuestStarterDatas;
        protected QuestStarterEnderParsing[] m_creatureQuestEnderDatas;

        protected SqlBuilder m_creatureTemplateBuilder;
        protected SqlBuilder m_creatureLocalesBuilder;
        protected SqlBuilder m_npcVendorBuilder;
        protected SqlBuilder m_creatureLootBuilder;
        protected SqlBuilder m_creatureReferenceLootBuilder;
        protected SqlBuilder m_creatureSkinningBuilder;
        protected SqlBuilder m_creaturePickpocketingBuilder;
        protected SqlBuilder m_creatureTrainerBuilder;
        protected SqlBuilder m_creatureQuestStarterBuilder;
        protected SqlBuilder m_creatureQuestEnderBuilder;
    }
}
