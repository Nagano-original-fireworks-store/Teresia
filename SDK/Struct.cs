using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDK
{
    public class Struct
    {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
        public class Login
        {
            [JsonProperty("account")]
            public Account Account { get; set; }

            //新设备登录 需要手机号验证（短信登录通道貌似不需要）
            [JsonProperty("device_grant_required")]
            public bool DeviceGrantRequired { get; set; }

            [JsonProperty("safe_mobile_required")]
            public bool SafeMobileRequired { get; set; }

            [JsonProperty("realperson_required")]
            public bool RealpersonRequired { get; set; }

            [JsonProperty("reactivate_required")]
            public bool ReactivateRequired { get; set; }

            [JsonProperty("realname_operation")]
            public string RealnameOperation { get; set; }
        }
        public class Account
        {
            [JsonProperty("uid")]
            public long Uid { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("mobile")]
            public string Mobile { get; set; }

            [JsonProperty("is_email_verify")]
            public string IsEmailVerify { get; set; } = "0";

            [JsonProperty("realname")]
            public string Realname { get; set; }

            [JsonProperty("identity_card")]
            public string IdentityCard { get; set; }

            [JsonProperty("token")]
            public string Token { get; set; }

            [JsonProperty("safe_mobile")]
            public string SafeMobile { get; set; }

            [JsonProperty("facebook_name")]
            public string FacebookName { get; set; }

            [JsonProperty("google_name")]
            public string GoogleName { get; set; }

            [JsonProperty("twitter_name")]
            public string TwitterName { get; set; }

            [JsonProperty("game_center_name")]
            public string GameCenterName { get; set; }

            [JsonProperty("apple_name")]
            public string AppleName { get; set; }

            [JsonProperty("sony_name")]
            public string SonyName { get; set; }

            [JsonProperty("tap_name")]
            public string TapName { get; set; }

            [JsonProperty("country")]
            public string Country { get; set; }

            [JsonProperty("reactivate_ticket")]
            public string ReactivateTicket { get; set; }

            [JsonProperty("area_code")]
            public string AreaCode { get; set; }

            [JsonProperty("device_grant_ticket")]
            public string DeviceGrantTicket { get; set; }

            [JsonProperty("steam_name")]
            public string SteamName { get; set; }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
        }

        public class ReqLogin
        {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
            [JsonProperty("account")]
            public string Account { get; set; }

            [JsonProperty("password")]
            public string Password { get; set; }

            [JsonProperty("is_crypto")]
            public bool IsCrypto { get; set; }
        }
    }
}
