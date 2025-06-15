using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BattleSystem;

namespace BattleSystem.Serialization
{
    public class SkillEffectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ISkillEffect) || objectType == typeof(List<ISkillEffect>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (objectType == typeof(List<ISkillEffect>))
            {
                var effects = new List<ISkillEffect>();
                var array = JArray.Load(reader);

                foreach (var item in array)
                {
                    effects.Add(CreateEffectFromJson(item, serializer));
                }

                return effects;
            }
            else
            {
                var obj = JObject.Load(reader);
                return CreateEffectFromJson(obj, serializer);
            }
        }

        private ISkillEffect CreateEffectFromJson(JToken token, JsonSerializer serializer)
        {
            // Verifica o ID do efeito para determinar qual tipo concreto instanciar
            var effectId = token["EffectId"]?.ToString().ToLowerInvariant();
            
            // Se não encontrar o EffectId, tenta identificar pelo contexto
            if (string.IsNullOrEmpty(effectId))
            {
                // Tenta identificar o tipo de efeito pelo conteúdo do JSON
                if (token["Damage"] != null && token["Damage"].Value<int>() > 0)
                    effectId = "damage";
                else if (token["Healing"] != null && token["Healing"].Value<int>() > 0)
                    effectId = "healing";
                else if (token["StatusEffects"] != null && token["StatusEffects"].HasValues)
                    effectId = "status_effect";
                else if (token["Type"] != null && token["Type"].ToString().Contains("Execute"))
                    effectId = "execute";
                else if (token["Type"] != null && token["Type"].ToString().Contains("Taunt"))
                    effectId = "taunt";
                else if (token["Type"] != null && token["Type"].ToString().Contains("Drain"))
                    effectId = "drain";
                else
                    effectId = "damage"; // Fallback padrão
            }

            ISkillEffect effect = effectId switch
            {
                "damage" => new DamageSkillEffect(),
                "healing" => new HealingSkillEffect(),
                "status_effect" => new StatusEffectSkillEffect(),
                "execute" => new ExecuteSkillEffect(),
                "taunt" => new TauntSkillEffect(),
                "drain" => new DrainSkillEffect(),
                _ => new DamageSkillEffect() // Fallback para efeito de dano como padrão
            };

            // Popula o objeto com as propriedades do JSON
            serializer.Populate(token.CreateReader(), effect);
            return effect;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is List<ISkillEffect> effects)
            {
                writer.WriteStartArray();
                foreach (var effect in effects)
                {
                    serializer.Serialize(writer, effect);
                }
                writer.WriteEndArray();
            }
            else if (value is ISkillEffect effect)
            {
                serializer.Serialize(writer, effect);
            }
        }
    }

    public class MessageProcessor
    {
        private void ProcessMessage(string message)
        {
            try
            {
                // Configurar o conversor personalizado para ISkillEffect
                var jsonSettings = new JsonSerializerSettings
                {
                    Converters = new List<JsonConverter> { new SkillEffectConverter() },
                    TypeNameHandling = TypeNameHandling.Auto
                };

                LogMessage($"Recebido: {message}");

                var baseResponse = JsonConvert.DeserializeObject<BaseResponse>(message, jsonSettings);

                if (!baseResponse.Success)
                {
                    LogMessage($"ERRO do servidor: {baseResponse.Message}");
                    return;
                }

                // Tratamento específico por tipo de mensagem
                if (message.Contains("\"level\"") && message.Contains("\"experienceToNextLevel\""))
                {
                    var expResponse = JsonConvert.DeserializeObject<ExperienceUpdateResponse>(message, jsonSettings);
                    HandleExperienceUpdate(expResponse);
                }
                else if (message.Contains("\"battleId\"") && !message.Contains("\"battle\""))
                {
                    var response = JsonConvert.DeserializeObject<CreateBattleResponse>(message, jsonSettings);
                    HandleCreateBattleResponse(response);
                }
                // ... resto do código
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao processar mensagem: {ex.Message}");
            }
        }

        private void LogMessage(string message)
        {
            // Implementação do log
        }

        private void HandleExperienceUpdate(ExperienceUpdateResponse response)
        {
            // Implementação do tratamento de atualização de experiência
        }

        private void HandleCreateBattleResponse(CreateBattleResponse response)
        {
            // Implementação do tratamento de resposta de criação de batalha
        }
    }
}