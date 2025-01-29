using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalRequests.MarkingCode;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.MarkingCode;

namespace TJConnector.StateSystem.Services.Contracts
{
    public interface IExternalEmission
    {
        // 1 запрос
        Task<CustomResult<DocumentCreateResponse>> CreateCodeEmission(EmissionCreateRequest body);
        // проверяем статус. если 1, то идем дальше. Если 0, выдаём ошибку.
        // 2 запрос
        Task<CustomResult<ProcessResponse>> ProcessCodeEmission(ProcessDocument body);
        // Проверяем статус. Если 4, то идем дальше. Если 5, выдаём ошибку. Если 3, пробуем проверить статус еще раз через 5 секунд.
        // 3 запрос. Заказ кодов.
        Task<CustomResult<EmissionCodesResponse>> GetCodesFromEmission(DownloadCodesRequest body);
        // проверить количество полученных кодов внутри. Если количество совпадает, заканчиваем заказ.
        // Если количество меньше. Ждем 10 секунд. Запрашиваем коды еще раз. Добавляем недостающие.

        Task<CustomResult<EmissionInfoResponse>> GetEmissionInfo(Guid uuid);

        Task<CustomResult<EmissionListResponse>> GetEmissionList(ListRequestRequest listRequestBody);
        Task<CustomResult<DocumentCreateResponse>> CreateCodeApplication(ApplicationCreateRequest body);
        Task<CustomResult<ProcessResponse>> ProcessCodeApplication(ProcessDocument body);
    }
}
