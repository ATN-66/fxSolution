namespace Common.Entities;

public enum DeInitReason
{
    The_EA_has_stopped_working_calling_the_ExpertRemove_function = 0,
    Program_removed_from_a_chart = 1,
    Program_recompiled = 2,
    A_symbol_or_a_chart_period_is_changed = 3,
    Chart_closed = 4,
    Inputs_changed_by_a_user = 5,
    Another_account_has_been_activated_or_reconnection_to_the_trade_server_has_occurred_due_to_changes_in_the_account_settings = 6,
    Another_chart_template_applied = 7,
    The_OnInit_handler_returned_a_non_zero_value = 8,
    Terminal_closed = 9
}